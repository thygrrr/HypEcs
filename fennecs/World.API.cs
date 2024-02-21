using System.Diagnostics.CodeAnalysis;
using fennecs.pools;

namespace fennecs;

public partial class World
{
    public bool IsAlive(Identity identity) => identity.IsEntity && _meta[identity.Id].Identity == identity;

    public EntityBuilder Spawn() => new(this, NewEntity());

    /// <summary>
    /// Schedule operations on the given identity, via fluid API.
    /// </summary>
    /// <example>
    /// <code>world.On(identity).Add(123).Add("string").Remove&lt;int&gt;();</code>
    /// </example>
    /// <remarks>
    /// The operations will be executed when this object is disposed, or the EntityBuilder's Id() method is called.
    /// </remarks>
    /// <param name="identity"></param>
    /// <returns>an EntityBuilder whose methods return itself, to provide a fluid syntax. </returns>
    public EntityBuilder On(Identity identity) => new(this, identity);

    #region Linked Components

    /* Idea for alternative API
    public struct Linked<T>(T link)
    {
        public readonly T Link = link;

        public static Linked<O> With<O>(O link) where O : class
        {
            return new Linked<O>(link);
        }

        public static Linked<Identity> With(Identity link)
        {
            return new Linked<Identity>(link);
        }
    }

    public void Add<T>(Identity identity, Linked<T> target) where T : class
    {
        var linkIdentity = _referenceStore.Request(target.Link);
        var typeExpression = TypeExpression.Create<T>(linkIdentity);
        AddComponent(identity, typeExpression, target);
    }
    */

    /// <summary>
    /// Creates an Archetype relation between this identity and an object (instance of a class).
    /// The relation is backed by the object itself, which will be enumerated by queries if desired.
    /// Whenever the identity is enumerated by a query, it will be batched only with other entities
    /// that share the exact relation, in addition to conforming with the other clauses of the query.
    /// The object is internally reference-counted, and the reference will be discarded once no other
    /// identity is linked to it.
    /// </summary>
    ///<remarks>
    /// Beware of Archetype Fragmentation!
    /// This feature is great to express relations between entities, but it will lead to
    /// fragmentation of the Archetype Graph, i.e. Archetypes with very few entities that
    /// are difficult to iterate over efficiently.
    /// </remarks>
    /// <param name="identity"></param>
    /// <param name="target"></param>
    /// <typeparam name="T"></typeparam>
    public void Link<T>(Identity identity, [NotNull] T target) where T : class
    {
        var typeExpression = TypeExpression.Create<T>(Identity.Of(target));
        AddComponent(identity, typeExpression, target);
    }
    
    /// <summary>
    /// Checks if this identity has an object-backed relation (instance of a class).
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="target"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool HasLink<T>(Identity identity, [NotNull] T target) where T : class
    {
        var typeExpression = TypeExpression.Create<T>(Identity.Of(target));
        return HasComponent(identity, typeExpression);
    }

    /// <summary>
    /// Removes the object-backed relation between this identity and the object (instance of a class).
    /// The object is internally reference-counted, and the reference will be discarded once no other
    /// identity is linked to it.
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="target"></param>
    /// <typeparam name="T"></typeparam>
    public void Unlink<T>(Identity identity, [NotNull] T target) where T : class
    {
        var typeExpression = TypeExpression.Create<T>(Identity.Of(target));
        RemoveComponent(identity, typeExpression);
    }


    /// <summary>
    /// Creates an Archetype relation between this identity and another identity.
    /// The relation is backed by an arbitrary type to provide additional data.
    /// Whenever the identity is enumerated by a query, it will be batched only with other entities
    /// that share the exact relation, in addition to conforming with the other clauses of the query.
    /// </summary>
    ///<remarks>
    /// Beware of Archetype Fragmentation!
    /// This feature is great to express relations between entities, but it will lead to
    /// fragmentation of the Archetype Graph, i.e. Archetypes with very few entities that
    /// are difficult to iterate over efficiently.
    /// </remarks>
    /// <param name="identity"></param>
    /// <param name="target"></param>
    /// <param name="data"></param>
    /// <typeparam name="T">any component type</typeparam>
    public void Link<T>(Identity identity, Identity target, T data)
    {
        var typeExpression = TypeExpression.Create<T>(target);
        AddComponent(identity, typeExpression, data);
    }
    
    /// <summary>
    /// Checks if this identity has a relation component with another identity.
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="target"></param>
    /// <typeparam name="T">any component type</typeparam>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public bool HasLink<T>(Identity identity, Identity target)
    {
        var typeExpression = TypeExpression.Create<T>(target);
        return HasComponent(identity, typeExpression);
    }

    /// <summary>
    /// Removes the relation component between this identity and another identity.
    /// </summary>
    /// <param name="identity"></param>
    /// <param name="target"></param>
    /// <typeparam name="T">any component type</typeparam>
    public void Unlink<T>(Identity identity, Identity target)
    {
        var typeExpression = TypeExpression.Create<T>(target);
        RemoveComponent(identity, typeExpression);
    }

    #endregion
    
    public void AddComponent<T>(Identity identity) where T : new()
    {
        var type = TypeExpression.Create<T>(Identity.None);
        AddComponent(identity, type, new T());
    }

    
    public void AddComponent<T>(Identity identity, T data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        var type = TypeExpression.Create<T>();
        AddComponent(identity, type, data);
    }

    
    public bool HasComponent<T>(Identity identity, Identity target = default)
    {
        var type = TypeExpression.Create<T>(target);
        return HasComponent(identity, type);
    }

    
    public void RemoveComponent<T>(Identity identity)
    {
        var type = TypeExpression.Create<T>(Identity.None);
        RemoveComponent(identity, type);
    }
    

    
    public void DespawnAllWith<T>(Identity target = default)
    {
        using var query = Query<Identity>().Has<T>(target).Build();
        query.Run(delegate(Span<Identity> entities)
        {
            foreach (var identity in entities) Despawn(identity);
        });
    }
    
    /*
    public IEnumerable<(TypeExpression, object)> GetComponents(Identity identity)
    {
        return GetComponents(identity);
    }
*/
    
    public World(int capacity = 4096)
    {
        _identityPool = new IdentityPool(capacity);
        _referenceStore = new ReferenceStore(capacity);
        
        _meta = new EntityMeta[capacity];

        //Create the "Identity" Archetype, which is also the root of the Archetype Graph.
        _root = AddTable([TypeExpression.Create<Identity>(Identity.None)]);
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

            // Find identity-identity relation reverse lookup (if applicable)
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
            throw new ArgumentException($"Identity {identity} already has component of type {typeExpression}");
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

        if (typeof(T) == typeof(Identity))
        {
            throw new TypeAccessException("Not allowed get mutable reference in root table (TypeExpression<Identity>, system integrity).");
        }

        var meta = _meta[identity.Id];
        var table = _tables[meta.TableId];
        var storage = table.GetStorage<T>(target);
        return ref storage[meta.Row];
    }
}
