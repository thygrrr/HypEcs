using System.Collections.Concurrent;
using System.Security.Claims;
using fennecs.pools;

namespace fennecs;

public partial class World
{
    public bool IsAlive(Identity identity) => identity.IsEntity && _meta[identity.Id].Identity == identity;

    public EntityBuilder Spawn() => new(this, NewEntity());

    /// <summary>
    /// Schedule operations on the given entity, via fluid API.
    /// </summary>
    /// <example>
    /// <code>world.On(entity).Add(123).Add("string").Remove&lt;int&gt;();</code>
    /// </example>
    /// <remarks>
    /// The operations will be executed when this object is disposed, or the EntityBuilder's Id() method is called.
    /// </remarks>
    /// <param name="entity"></param>
    /// <returns>an EntityBuilder whose methods return itself, to provide a fluid syntax. </returns>
    public EntityBuilder On(Entity entity) => new(this, entity);

    #region Linked Components 
    
    public void Link<T>(Entity entity, T target) where T : class
    {
        var typeExpression = TypeExpression.Create<T>(Identity.Of(target));
        AddComponent(entity, typeExpression, target);
    }

    public void Unlink<T>(Entity entity, T target) where T : class
    {
        var typeExpression = TypeExpression.Create<T>(Identity.Of(target));
        RemoveComponent(entity, typeExpression);
    }

    
    public void Link<T>(Entity entity, Entity target)
    {
        var typeExpression = TypeExpression.Create<T>(target.Identity);
        AddComponent(entity, typeExpression, target);
    }

    
    public void Unlink<T>(Entity entity, Entity target)
    {
        var typeExpression = TypeExpression.Create<T>(target.Identity);
        RemoveComponent(entity, typeExpression);
    }

    #endregion

/*
    public void Unlink<T>(Entity entity, Identity target)
    {
        var type = TypeExpression.Create<T>(target);
        RemoveComponent(entity, type);
    }
*/  
    
    public void DespawnAllWith<T>()
    {
        var query = Query<Entity>().Has<T>().Build();
        query.Run(delegate (Span<Entity> entities)
        {
            foreach (var entity in entities) Despawn(entity);
        });
    }

    public bool HasComponent<T>(Entity entity)
    {
        var type = TypeExpression.Create<T>(Identity.None);
        return HasComponent(entity, type);
    }

    public void AddComponent<T>(Entity entity) where T : new()
    {
        var type = TypeExpression.Create<T>(Identity.None);
        AddComponent(entity.Identity, type, new T());
    }

    public void AddComponent<T>(Entity entity, T data, Identity target = default) where T : notnull
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        var type = TypeExpression.Create<T>(target);
        AddComponent(entity.Identity, type, data);
    }

    public void RemoveComponent<T>(Entity entity)
    {
        var type = TypeExpression.Create<T>(Identity.None);
        RemoveComponent(entity.Identity, type);
    }

    public void RemoveComponent<T>(Entity entity, Type target)
    {
        var type = TypeExpression.Create<T>(new Identity(target));
        RemoveComponent(entity, type);
    }

    public IEnumerable<(TypeExpression, object)> GetComponents(Entity entity)
    {
        return GetComponents(entity.Identity);
    }

    public bool TryGetComponent<T>(Entity entity, out Ref<T> component)
    {
        if (!HasComponent<T>(entity))
        {
            component = default;
            return false;
        }

        component = new Ref<T>(ref GetComponent<T>(entity.Identity, Identity.None));
        return true;
    }

    public bool TryGetComponent<T>(Entity entity, Identity target, out Ref<T> component)
    {
        if (!HasComponent<T>(entity, target))
        {
            component = default;
            return false;
        }

        component = new Ref<T>(ref GetComponent<T>(entity.Identity, target));
        return true;
    }

    public bool HasComponent<T>(Entity entity, Entity target)
    {
        var type = TypeExpression.Create<T>(target.Identity);
        return HasComponent(entity.Identity, type);
    }

    public bool HasComponent<T>(Entity entity, Type target)
    {
        var type = TypeExpression.Create<T>(new Identity(target));
        return HasComponent(entity.Identity, type);
    }

    public bool HasComponent<T, Target>(Entity entity)
    {
        var type = TypeExpression.Create<T>(new Identity(LanguageType<Target>.Id));
        return HasComponent(entity.Identity, type);
    }

    public void AddComponent<T>(Entity entity, Entity target) where T : new()
    {
        var type = TypeExpression.Create<T>(target.Identity);
        AddComponent(entity.Identity, type, new T());
    }
    
    public void AddComponent<T>(Entity entity, T data, Entity target)
    {
        var type = TypeExpression.Create<T>(target.Identity);
        AddComponent(entity.Identity, type, data);
    }

    public void RemoveComponent(Entity entity, Type type, Entity target)
    {
        var typeExpression = TypeExpression.Create(type, target.Identity);
        RemoveComponent(entity.Identity, typeExpression);
    }

    #region QueryBuilders

    public QueryBuilder<Entity> Query()
    {
        return new QueryBuilder<Entity>(this);
    }

    public QueryBuilder<C> Query<C>()
    {
        return new QueryBuilder<C>(this);
    }

    public QueryBuilder<C1, C2> Query<C1, C2>()
    {
        return new QueryBuilder<C1, C2>(this);
    }

    public QueryBuilder<C1, C2, C3> Query<C1, C2, C3>()
    {
        return new QueryBuilder<C1, C2, C3>(this);
    }

    public QueryBuilder<C1, C2, C3, C4> Query<C1, C2, C3, C4>()
    {
        return new QueryBuilder<C1, C2, C3, C4>(this);
    }

    public QueryBuilder<C1, C2, C3, C4, C5> Query<C1, C2, C3, C4, C5>()
    {
        return new QueryBuilder<C1, C2, C3, C4, C5>(this);
    }

    #endregion

    #region Archetypes

    public World(int capacity = 4096)
    {
        _identityPool = new IdentityPool(capacity);
        _referenceStore = new ReferenceStore(capacity);
        
        _meta = new EntityMeta[capacity];

        //Create the "Entity" Archetype, which is also the root of the Archetype Graph.
        _root = AddTable([TypeExpression.Create<Entity>(Identity.None)]);
    }

    public void Despawn(Identity identity)
    {
        lock (_spawnLock)
        {
            AssertAlive(identity);

            if (_mode == Mode.Deferred)
            {
                _deferredOperations.Enqueue(new DeferredOperation {Code = OpCode.Despawn, Identity = identity});
                return;
            }

            ref var meta = ref _meta[identity.Id];

            var table = _tables[meta.TableId];
            table.Remove(meta.Row);
            meta.Clear();

            _identityPool.Despawn(identity);

            // Find entity-entity relation reverse lookup (if applicable)
            if (!_typesByRelationTarget.TryGetValue(identity, out var list)) return;

            //Remove components from all entities that had a relation
            foreach (var type in list)
            {
                var tablesWithType = _tablesByType[type];

                //TODO: There should be a bulk remove method instead.
                foreach (var tableWithType in tablesWithType)
                {
                    for (var i = tableWithType.Count - 1; i >= 0; i--)
                    {
                        RemoveComponent(tableWithType.Identities[i], type);
                    }
                }
            }
        }
    }

    private void AddComponent<T>(Identity identity, TypeExpression typeExpression, T data)
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
            _deferredOperations.Enqueue(new DeferredOperation {Code = OpCode.Add, Identity = identity, TypeExpression = typeExpression, Data = data!});
            return;
        }

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

    public ref T GetComponent<T>(Identity identity, Identity target = default)
    {
        AssertAlive(identity);

        var type = TypeExpression.Create<T>(target);
        var meta = _meta[identity.Id];
        var table = _tables[meta.TableId];
        var storage = (T[]) table.GetStorage(type);
        return ref storage[meta.Row];
    }

    private bool HasComponent(Identity identity, TypeExpression typeExpression)
    {
        var meta = _meta[identity.Id];
        return meta.Identity != Identity.None
               && meta.Identity == identity
               && typeExpression.Matches(_tables[meta.TableId].Types);
    }

    private void RemoveComponent(Identity identity, TypeExpression typeExpression)
    {
        if (_mode == Mode.Deferred)
        {
            _deferredOperations.Enqueue(new DeferredOperation {Code = OpCode.Remove, Identity = identity, TypeExpression = typeExpression});
            return;
        }

        ref var meta = ref _meta[identity.Id];
        var oldTable = _tables[meta.TableId];

        if (!oldTable.Types.Contains(typeExpression))
        {
            throw new ArgumentException($"cannot remove non-existent component {typeExpression} from entity {identity}");
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
        }

        var newRow = Table.MoveEntry(identity, meta.Row, oldTable, newTable);

        meta.Row = newRow;
        meta.TableId = newTable.Id;
    }

    private void Apply(ConcurrentQueue<DeferredOperation> operations)
    {
        while (operations.TryDequeue(out var op))
        {
            AssertAlive(op.Identity);

            switch (op.Code)
            {
                case OpCode.Add:
                    AddComponent(op.Identity, op.TypeExpression, op.Data);
                    break;
                case OpCode.Remove:
                    RemoveComponent(op.Identity, op.TypeExpression);
                    break;
                case OpCode.Despawn:
                    Despawn(op.Identity);
                    break;
            }
        }
    }

    #endregion

    public struct DeferredOperation
    {
        public required OpCode Code;
        public TypeExpression TypeExpression;
        public Identity Identity;
        public object Data;
    }

    public enum OpCode
    {
        Add,
        Remove,
        Despawn,
    }
}
