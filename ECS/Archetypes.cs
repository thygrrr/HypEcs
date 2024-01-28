using System.Collections.Concurrent;

// ReSharper disable ReturnTypeCanBeEnumerable.Global

namespace ECS;

public sealed class Archetypes
{
	internal EntityMeta[] Meta = new EntityMeta[512];

	internal readonly Queue<Identity> UnusedIds = new();

	internal readonly List<Table> Tables = new();

	internal readonly Dictionary<int, Query> Queries = new();

	internal int EntityCount;

	private readonly ConcurrentQueue<DeferredOperation> _tableOperations = new();
	private readonly Dictionary<Type, Entity> _typeEntities = new();
	internal readonly Dictionary<TypeExpression, List<Table>> TablesByType = new();
	private readonly Dictionary<Identity, HashSet<TypeExpression>> _typesByRelationTarget = new();
	private readonly Dictionary<TypeFamily, HashSet<Entity>> _targetsByRelationType = new();
	private readonly Dictionary<int, HashSet<TypeExpression>> _relationsByTypes = new();

	private int _lockCount;
	private bool _isLocked;

	public Archetypes()
	{
		AddTable(new SortedSet<TypeExpression> { TypeExpression.Create<Entity>(Identity.None) });
	}

	
	public Entity Spawn()
	{
		var identity = UnusedIds.Count > 0 ? UnusedIds.Dequeue() : new Identity(++EntityCount);

		var table = Tables[0];

		var row = table.Add(identity);

		if (Meta.Length == EntityCount) Array.Resize(ref Meta, EntityCount << 1);

		Meta[identity.Id] = new EntityMeta(identity, table.Id, row);

		var entity = new Entity(identity);

		var entityStorage = (Entity[])table.Storages[0];
		entityStorage[row] = entity;

		return entity;
	}

	
	public void Despawn(Identity identity)
	{
		if (!IsAlive(identity)) return;

		if (_isLocked)
		{
			_tableOperations.Enqueue(new DeferredOperation { Operation = TableOp.Despawn, Identity = identity });
			return;
		}

		ref var meta = ref Meta[identity.Id];

		var table = Tables[meta.TableId];

		table.Remove(meta.Row);

		meta.Row = 0;
		meta.Identity = Identity.None;

		UnusedIds.Enqueue(new Identity(identity.Id, (ushort) (identity.Generation + 1)));

		//Remove components from all entities that had a relation pointing to the despawned entity
		if (!_typesByRelationTarget.TryGetValue(identity, out var list))
		{
			return;
		}

		foreach (var type in list)
		{
			_targetsByRelationType[type].Remove(identity);
			
			var tablesWithType = TablesByType[type];

			foreach (var tableWithType in tablesWithType)
			{
				for (var i = 0; i < tableWithType.Count; i++)
				{
					RemoveComponent(type, tableWithType.Identities[i]);
				}
			}
		}
	}

	
	public void AddComponent<T>(TypeExpression type_expression, Identity identity, T data, Entity target = default)
	{
		AssertAlive(identity);

		ref var meta = ref Meta[identity.Id];
		var oldTable = Tables[meta.TableId];

		if (oldTable.Types.Contains(type_expression))
		{
			throw new ArgumentException($"Entity {identity} already has component of type {type_expression}");
		}

		if (_isLocked)
		{
			_tableOperations.Enqueue(new DeferredOperation { Operation = TableOp.Add, Identity = identity, TypeExpression = type_expression, Data = data! });
			return;
		}

		if (!_targetsByRelationType.ContainsKey(type_expression))
		{
			_targetsByRelationType[type_expression] = new ();
		}
		_targetsByRelationType[type_expression].Add(identity);
		
		
		var oldEdge = oldTable.GetTableEdge(type_expression);

		var newTable = oldEdge.Add;

		if (newTable == null)
		{
			var newTypes = oldTable.Types.ToList();
			newTypes.Add(type_expression);
			newTable = AddTable(new SortedSet<TypeExpression>(newTypes));
			oldEdge.Add = newTable;

			var newEdge = newTable.GetTableEdge(type_expression);
			newEdge.Remove = oldTable;
		}

		var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);

		meta.Row = newRow;
		meta.TableId = newTable.Id;

		var storage = newTable.GetStorage(type_expression);
		storage.SetValue(data, newRow);
	}

	
	public ref T GetComponent<T>(Identity identity, Identity target)
	{
		AssertAlive(identity);

		var type = TypeExpression.Create<T>(target);
		var meta = Meta[identity.Id];
		AssertEqual(meta.Identity, identity);
		var table = Tables[meta.TableId];
		var storage = (T[])table.GetStorage(type);
		return ref storage[meta.Row];
	}


	
	public bool HasComponent(TypeExpression type_expression, Identity identity)
	{
		var meta = Meta[identity.Id];
		return meta.Identity != Identity.None
			   && meta.Identity == identity
			   && Tables[meta.TableId].Types.Contains(type_expression);
	}

	
	public void RemoveComponent(TypeExpression type_expression, Identity identity)
	{
		ref var meta = ref Meta[identity.Id];
		var oldTable = Tables[meta.TableId];

		if (!oldTable.Types.Contains(type_expression))
		{
			throw new ArgumentException($"cannot remove non-existent component {type_expression.Type.Name} from entity {identity}");
		}

		if (_isLocked)
		{
			_tableOperations.Enqueue(new DeferredOperation { Operation = TableOp.Remove, Identity = identity, TypeExpression = type_expression });
			return;
		}
		

		// could be _targetsByRelationType[type.Wildcard()].Remove(identity);
		//(with enough unit test coverage)
		if (_targetsByRelationType.TryGetValue(type_expression, out var targetSet))
		{
			targetSet.Remove(identity);
		}
		
		var oldEdge = oldTable.GetTableEdge(type_expression);

		var newTable = oldEdge.Remove;

		if (newTable == null)
		{
			var newTypes = oldTable.Types.ToList();
			newTypes.Remove(type_expression);
			newTable = AddTable(new SortedSet<TypeExpression>(newTypes));
			oldEdge.Remove = newTable;

			var newEdge = newTable.GetTableEdge(type_expression);
			newEdge.Add = oldTable;

			//Tables.Add(newTable); <-- already added in AddTable
		}

		var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);

		meta.Row = newRow;
		meta.TableId = newTable.Id;
	}

	
	public Query GetQuery(Mask mask, Func<Archetypes, Mask, List<Table>, Query> createQuery)
	{
		var hash = mask.GetHashCode();

		if (Queries.TryGetValue(hash, out var query))
		{
			MaskPool.Add(mask);
			return query;
		}

		var matchingTables = new List<Table>();

		var type = mask.HasTypes[0];
		if (!TablesByType.TryGetValue(type, out var typeTables))
		{
			typeTables = new List<Table>();
			TablesByType[type] = typeTables;
		}

		foreach (var table in typeTables)
		{
			if (!IsMaskCompatibleWith(mask, table)) continue;

			matchingTables.Add(table);
		}

		query = createQuery(this, mask, matchingTables);
		Queries.Add(hash, query);

		return query;
	}

	
	internal bool IsMaskCompatibleWith(Mask mask, Table table)
	{
		var has = ListPool<TypeExpression>.Get();
		var not = ListPool<TypeExpression>.Get();
		var any = ListPool<TypeExpression>.Get();

		var hasAnyTarget = ListPool<TypeExpression>.Get();
		var notAnyTarget = ListPool<TypeExpression>.Get();
		var anyAnyTarget = ListPool<TypeExpression>.Get();

		foreach (var type in mask.HasTypes)
		{
			if (type.Identity == Identity.Any) hasAnyTarget.Add(type);
			else has.Add(type);
		}

		foreach (var type in mask.NotTypes)
		{
			if (type.Identity == Identity.Any) notAnyTarget.Add(type);
			else not.Add(type);
		}

		foreach (var type in mask.AnyTypes)
		{
			if (type.Identity == Identity.Any) anyAnyTarget.Add(type);
			else any.Add(type);
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
		ListPool<TypeExpression>.Add(anyAnyTarget);

		return matchesComponents && matchesRelation;
	}

	
	internal bool IsAlive(Identity identity)
	{
		return identity != Identity.None && Meta[identity.Id].Identity == identity;
	}

	
	internal ref EntityMeta GetEntityMeta(Identity identity)
	{
		return ref Meta[identity.Id];
	}

	
	internal Table GetTable(int tableId)
	{
		return Tables[tableId];
	}

	
	internal Entity GetTarget(TypeExpression type_expression, Identity identity)
	{
		var meta = Meta[identity.Id];
		var table = Tables[meta.TableId];

		foreach (var storageType in table.Types)
		{
			if (!storageType.IsRelation || storageType.TypeId != type_expression.TypeId) continue;
			return new Entity(storageType.Identity);
		}

		return Entity.None;
	}

	
	internal Entity[] GetTargets(TypeExpression type_expression, Identity identity)
	{
		if (identity == Identity.Any)
		{
			return _targetsByRelationType.TryGetValue(type_expression, out var entitySet)
				? entitySet.ToArray()
				: Array.Empty<Entity>();
		}

		AssertAlive(identity);

		var list = ListPool<Entity>.Get();
		var meta = Meta[identity.Id];
		var table = Tables[meta.TableId];
		foreach (var storageType in table.Types)
		{
			if (!storageType.IsRelation || storageType.TypeId != type_expression.TypeId) continue;
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

		var meta = Meta[identity.Id];
		var table = Tables[meta.TableId];


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
		var table = new Table(Tables.Count, this, types);
		Tables.Add(table);

		foreach (var type in types)
		{
			if (!TablesByType.TryGetValue(type, out var tableList))
			{
				tableList = new List<Table>();
				TablesByType[type] = tableList;
			}

			tableList.Add(table);

			if (!type.IsRelation) continue;

			if (!_typesByRelationTarget.TryGetValue(type.Identity, out var typeList))
			{
				typeList = new HashSet<TypeExpression>();
				_typesByRelationTarget[type.Identity] = typeList;
			}

			typeList.Add(type);
			
			if (!_relationsByTypes.TryGetValue(type.TypeId, out var relationTypeSet))
			{
				relationTypeSet = new HashSet<TypeExpression>();
				_relationsByTypes[type.TypeId] = relationTypeSet;
			}

			relationTypeSet.Add(type);
		}

		foreach (var query in Queries.Values.Where(query => IsMaskCompatibleWith(query.Mask, table)))
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


	private void ApplyTableOperations()
	{
		foreach (var op in _tableOperations)
		{
			AssertAlive(op.Identity);

			switch (op.Operation)
			{
				case TableOp.Add:
					AddComponent(op.TypeExpression, op.Identity, op.Data);
					break;
				case TableOp.Remove:
					RemoveComponent(op.TypeExpression, op.Identity);
					break;
				case TableOp.Despawn:
					Despawn(op.Identity);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		_tableOperations.Clear();
	}


	public void Lock()
	{
		_lockCount++;
		_isLocked = true;
	}

	
	public void Unlock()
	{
		_lockCount--;
		if (_lockCount != 0) return;
		_isLocked = false;

		ApplyTableOperations();
	}

	private struct DeferredOperation
	{
		public required TableOp Operation;
		public TypeExpression TypeExpression;
		public Identity Identity;
		public object Data;
	}

	private enum TableOp
	{
		Add,
		Remove,
		Despawn
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
