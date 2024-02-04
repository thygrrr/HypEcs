// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ReturnTypeCanBeEnumerable.Global

namespace ECS;

public sealed class Archetypes
{
	private EntityMeta[] _meta = new EntityMeta[512];
	private readonly List<Table> _tables = [];
	private readonly Dictionary<int, Query> _queries = new();
	
	
	private readonly ConcurrentBag<Identity> _unusedIds = [];
	private Table entityRoot => _tables[0];
	private int _entityCount;

	
	private readonly ConcurrentQueue<DeferredOperation> _deferredOperations = new();
	private readonly Dictionary<Type, Entity> _typeEntities = new();
	private readonly Dictionary<TypeExpression, List<Table>> _tablesByType = new();
	private readonly Dictionary<Identity, HashSet<TypeExpression>> _typesByRelationTarget = new();
	private readonly Dictionary<TypeFamily, HashSet<Entity>> _targetsByRelationType = new();
	private readonly Dictionary<int, HashSet<TypeExpression>> _relationsByTypes = new();

	private readonly object _modeChangeLock = new();
	private int _lockCount;

	private Mode _mode = Mode.Immediate;

	public Archetypes()
	{
		AddTable([TypeExpression.Create<Entity>(Identity.None)]);
	}

	
	public Entity Spawn()
	{
		if (!_unusedIds.TryTake(out var identity))
		{
			identity = new Identity(++_entityCount);
		}


		var row = entityRoot.Add(identity);

		if (_meta.Length == _entityCount) Array.Resize(ref _meta, _entityCount * 2);

		_meta[identity.Id] = new EntityMeta(identity, entityRoot.Id, row);

		var entity = new Entity(identity);

		var entityStorage = (Entity[])entityRoot.Storages[0];
		entityStorage[row] = entity;

		return entity;
	}

	
	public void Despawn(Identity identity)
	{
		if (!IsAlive(identity)) return;

		if (_mode == Mode.Deferred)
		{
			_deferredOperations.Enqueue(new DeferredOperation { Operation = Deferred.Despawn, Identity = identity });
			return;
		}

		ref var meta = ref _meta[identity.Id];

		var table = _tables[meta.TableId];
		table.Remove(meta.Row);
		meta.Clear();

		_unusedIds.Add(new Identity(identity.Id, (ushort) (identity.Generation + 1)));

		
		// Find entity-entity relation reverse lookup (if applicable)
		if (!_typesByRelationTarget.TryGetValue(identity, out var list)) return;

		//Remove components from all entities that had a relation
		foreach (var type in list)
		{
			_targetsByRelationType[type].Remove(identity);
			
			var tablesWithType = _tablesByType[type];

			foreach (var tableWithType in tablesWithType)
			{
				//TODO: There should be a bulk remove method instead.
				for (var i = 0; i < tableWithType.Count; i++)
				{
					RemoveComponent(type, tableWithType.Identities[i]);
				}
			}
		}
	}

	
	public void AddComponent<T>(TypeExpression typeExpression, Identity identity, T data, Entity target = default)
	{
		AssertAlive(identity);

		ref var meta = ref _meta[identity.Id];
		var oldTable = _tables[meta.TableId];

		if (oldTable.Types.Contains(typeExpression))
		{
			throw new ArgumentException($"Entity {identity} already has component of type {typeExpression}");
		}

		if (_mode == Mode.Deferred)
		{
			_deferredOperations.Enqueue(new DeferredOperation { Operation = Deferred.Add, Identity = identity, TypeExpression = typeExpression, Data = data! });
			return;
		}

		if (!_targetsByRelationType.ContainsKey(typeExpression))
		{
			_targetsByRelationType[typeExpression] = [];
		}
		_targetsByRelationType[typeExpression].Add(identity);
		
		
		var oldEdge = oldTable.GetTableEdge(typeExpression);

		var newTable = oldEdge.Add;

		if (newTable == null)
		{
			var newTypes = oldTable.Types.ToList();
			newTypes.Add(typeExpression);
			newTable = AddTable(new SortedSet<TypeExpression>(newTypes));
			oldEdge.Add = newTable;

			var newEdge = newTable.GetTableEdge(typeExpression);
			newEdge.Remove = oldTable;
		}

		var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);

		meta.Row = newRow;
		meta.TableId = newTable.Id;

		var storage = newTable.GetStorage(typeExpression);
		storage.SetValue(data, newRow);
	}

	
	public ref T GetComponent<T>(Identity identity, Identity target)
	{
		AssertAlive(identity);

		var type = TypeExpression.Create<T>(target);
		var meta = _meta[identity.Id];
		AssertEqual(meta.Identity, identity);
		var table = _tables[meta.TableId];
		var storage = (T[])table.GetStorage(type);
		return ref storage[meta.Row];
	}


	
	public bool HasComponent(TypeExpression typeExpression, Identity identity)
	{
		var meta = _meta[identity.Id];
		return meta.Identity != Identity.None
			   && meta.Identity == identity
			   && _tables[meta.TableId].Types.Contains(typeExpression);
	}

	
	public void RemoveComponent(TypeExpression typeExpression, Identity identity)
	{
		ref var meta = ref _meta[identity.Id];
		var oldTable = _tables[meta.TableId];

		if (!oldTable.Types.Contains(typeExpression))
		{
			throw new ArgumentException($"cannot remove non-existent component {typeExpression.Type.Name} from entity {identity}");
		}

		if (_mode == Mode.Deferred)
		{
			_deferredOperations.Enqueue(new DeferredOperation { Operation = Deferred.Remove, Identity = identity, TypeExpression = typeExpression });
			return;
		}
		

		// could be _targetsByRelationType[type.Wildcard()].Remove(identity);
		//(with enough unit test coverage)
		if (_targetsByRelationType.TryGetValue(typeExpression, out var targetSet))
		{
			targetSet.Remove(identity);
		}
		
		var oldEdge = oldTable.GetTableEdge(typeExpression);

		var newTable = oldEdge.Remove;

		if (newTable == null)
		{
			var newTypes = oldTable.Types.ToList();
			newTypes.Remove(typeExpression);
			newTable = AddTable(new SortedSet<TypeExpression>(newTypes));
			oldEdge.Remove = newTable;

			var newEdge = newTable.GetTableEdge(typeExpression);
			newEdge.Add = oldTable;

			//Tables.Add(newTable); <-- already added in AddTable
		}

		var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);

		meta.Row = newRow;
		meta.TableId = newTable.Id;
	}

	public void DiscardQuery(Mask mask)
	{
		_queries.Remove(mask);
		MaskPool.Add(mask);
	}
	
	public Query GetQuery(Mask mask, Func<Archetypes, Mask, List<Table>, Query> createQuery)
	{
		if (_queries.TryGetValue(mask, out var query))
		{
			MaskPool.Add(mask);
			return query;
		}

		var type = mask.HasTypes[0];
		if (!_tablesByType.TryGetValue(type, out var typeTables))
		{
			typeTables = [];
			_tablesByType[type] = typeTables;
		}

		var matchingTables = typeTables
			.Where(table => IsMaskCompatibleWith(mask, table))
			.ToList();
		
		query = createQuery(this, mask, matchingTables);
		
		_queries.Add(mask, query);
		return query;
	}


	private bool IsMaskCompatibleWith(Mask mask, Table table)
	{
		var has = ListPool<TypeExpression>.Get();
		var not = ListPool<TypeExpression>.Get();
		var any = ListPool<TypeExpression>.Get();

		var hasAnyTarget = ListPool<TypeExpression>.Get();
		var notAnyTarget = ListPool<TypeExpression>.Get();

		foreach (var type in mask.HasTypes)
		{
			if (type.Identity == Identity.Any) hasAnyTarget.Add(type);
			else has.Add(type);
		}

		foreach (var type in mask.NotTypes)
		{
			//if (type.Identity == Identity.Any) notAnyTarget.Add(type);
			//else TODO: Find out if we can make "Not Any" actually a valid query. 
			//This case then could go into a special function
			not.Add(type);
		}

		foreach (var type in mask.AnyTypes)
		{
			any.Add(type);
		}

		var matchesComponents = table.Types.IsSupersetOf(has);
		matchesComponents &= !table.Types.Overlaps(not);
		matchesComponents &= mask.AnyTypes.Count == 0 || table.Types.Overlaps(any);

		var matchesRelation = true;

		foreach (var type in hasAnyTarget)
		{
			if (!_relationsByTypes.TryGetValue(type.TypeId, out var list))
			{
				matchesRelation = false;
				continue;
			}

			matchesRelation &= table.Types.Overlaps(list);
		}

		ListPool<TypeExpression>.Add(has);
		ListPool<TypeExpression>.Add(not);
		ListPool<TypeExpression>.Add(any);
		
		ListPool<TypeExpression>.Add(hasAnyTarget);
		ListPool<TypeExpression>.Add(notAnyTarget);

		return matchesComponents && matchesRelation;
	}

	
	internal bool IsAlive(Identity identity)
	{
		return identity != Identity.None && _meta[identity.Id].Identity == identity;
	}

	
	internal ref EntityMeta GetEntityMeta(Identity identity)
	{
		return ref _meta[identity.Id];
	}

	
	internal Table GetTable(int tableId)
	{
		return _tables[tableId];
	}

	
	internal Entity GetTarget(TypeExpression typeExpression, Identity identity)
	{
		var meta = _meta[identity.Id];
		var table = _tables[meta.TableId];

		foreach (var storageType in table.Types)
		{
			if (!storageType.IsRelation || storageType.TypeId != typeExpression.TypeId) continue;
			return new Entity(storageType.Identity);
		}

		return Entity.None;
	}

	
	internal Entity[] GetTargets(TypeExpression typeExpression, Identity identity)
	{
		if (identity == Identity.Any)
		{
			return _targetsByRelationType.TryGetValue(typeExpression, out var entitySet)
				? entitySet.ToArray()
				: Array.Empty<Entity>();
		}

		AssertAlive(identity);

		var list = ListPool<Entity>.Get();
		var meta = _meta[identity.Id];
		var table = _tables[meta.TableId];
		foreach (var storageType in table.Types)
		{
			if (!storageType.IsRelation || storageType.TypeId != typeExpression.TypeId) continue;
			list.Add(new Entity(storageType.Identity));
		}

		var targetEntities = list.ToArray();
		ListPool<Entity>.Add(list);

		return targetEntities;
	}

	
	internal (TypeExpression, object)[] GetComponents(Identity identity)
	{
		AssertAlive(identity);

		var list = ListPool<(TypeExpression, object)>.Get();

		var meta = _meta[identity.Id];
		var table = _tables[meta.TableId];


		foreach (var type in table.Types)
		{
			var storage = table.GetStorage(type);
			list.Add((type, storage.GetValue(meta.Row)!));
		}

		var array = list.ToArray();
		ListPool<(TypeExpression, object)>.Add(list);
		return array;
	}

	
	private Table AddTable(SortedSet<TypeExpression> types)
	{
		var table = new Table(_tables.Count, this, types);
		_tables.Add(table);

		foreach (var type in types)
		{
			if (!_tablesByType.TryGetValue(type, out var tableList))
			{
				tableList = [];
				_tablesByType[type] = tableList;
			}

			tableList.Add(table);

			if (!type.IsRelation) continue;

			if (!_typesByRelationTarget.TryGetValue(type.Identity, out var typeList))
			{
				typeList = [];
				_typesByRelationTarget[type.Identity] = typeList;
			}

			typeList.Add(type);
			
			if (!_relationsByTypes.TryGetValue(type.TypeId, out var relationTypeSet))
			{
				relationTypeSet = [];
				_relationsByTypes[type.TypeId] = relationTypeSet;
			}

			relationTypeSet.Add(type);
		}

		foreach (var query in _queries.Values.Where(query => IsMaskCompatibleWith(query.Mask, table)))
		{
			query.AddTable(table);
		}

		return table;
	}

	
	internal Entity GetTypeEntity(Type type)
	{
		if (!_typeEntities.TryGetValue(type, out var entity))
		{
			entity = Spawn();
			_typeEntities.Add(type, entity);
		}

		return entity;
	}


	private void ApplyDeferredOperations()
	{
		foreach (var op in _deferredOperations)
		{
			AssertAlive(op.Identity);

			switch (op.Operation)
			{
				case Deferred.Add:
					AddComponent(op.TypeExpression, op.Identity, op.Data);
					break;
				case Deferred.Remove:
					RemoveComponent(op.TypeExpression, op.Identity);
					break;
				case Deferred.Despawn:
					Despawn(op.Identity);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		_deferredOperations.Clear();
	}


	public void Lock()
	{
		lock (_modeChangeLock)
		{
			if (_mode != Mode.Immediate) throw new InvalidOperationException("Archetypes: Lock called while not in immediate (default) mode");
			
			_lockCount++;
			_mode = Mode.Deferred;
		}
	}
	
	public void Unlock()
	{
		lock (_modeChangeLock)
		{
			if (_mode != Mode.Deferred) throw new InvalidOperationException("Archetypes: Unlock called while not in deferred mode");
			
			_lockCount--;
			
			if (_lockCount != 0) return;
			
			_mode = Mode.Immediate;
			ApplyDeferredOperations();
		}
	}

	private enum Mode
	{
		Immediate = default,
		Deferred,
		//Bulk
	}
	
	private struct DeferredOperation
	{
		public required Deferred Operation;
		public TypeExpression TypeExpression;
		public Identity Identity;
		public object Data;
	}

	private enum Deferred
	{
		Add,
		Remove,
		Despawn,
	}


	#region Assert Helpers

	
	private void AssertAlive(Identity identity)
	{
		if (!IsAlive(identity))
		{
			throw new Exception($"Entity {identity} is not alive.");
		}
	}

	
	private static void AssertEqual(Identity metaIdentity, Identity identity)
	{
		if (metaIdentity != identity)
		{
			throw new Exception($"Entity {identity} meta/generation mismatch.");
		}
	}
	#endregion
}
