using fennecs.pools;

namespace fennecs;

public readonly struct EntityBuilder(World world, Entity entity) : IDisposable
{
    private readonly PooledList<World.DeferredOperation> _operations = PooledList<World.DeferredOperation>.Rent();
    
    public EntityBuilder Add<T>(Entity target = default) where T : new() 
    {
        if (target.Identity == Identity.Any) throw new InvalidOperationException("EntityBuilder: Cannot relate to Identity.Any.");

        world.AddComponent<T>(entity, target);
        
        /*
         TODO: Change to this pattern.
        _operations.Add(
            new World.DeferredOperation()
            {
                Operation = World.Operation.Add,
                Identity = entity,
                Data = target,
            });
        */
        return this;
    }

    public EntityBuilder Add<T>(Type type) where T : new()
    {
        world.AddComponent<T>(entity, new Identity(type));
        return this;
    }
    
    public EntityBuilder Add<T>(T data) where T : notnull
    {
        world.AddComponent(entity, data);
        return this;
    }

    
    public EntityBuilder Add<T>(T data, Entity target) 
    {
        if (target.Identity == Identity.Any) throw new InvalidOperationException("EntityBuilder: Cannot relate to Identity.Any.");
        
        world.AddComponent(entity, data, target);
        return this;
    }

    
    public EntityBuilder Add<T>(T data, Type target) where T : notnull
    {
        world.AddComponent(entity, data, new Identity(target));
        return this;
    }

    
    public EntityBuilder Remove<T>() 
    {
        world.RemoveComponent<T>(entity);
        return this;
    }

    
    public EntityBuilder Remove<T>(Entity target) 
    {
        world.RemoveComponent<T>(entity, target);
        return this;
    }

    
    public EntityBuilder Remove<T>(Type target) 
    {
        world.RemoveComponent<T>(entity, new Identity(target));
        return this;
    }

    public Entity Id()
    {
        Dispose();
        return entity;
    }

    public void Dispose()
    {
        _operations.Dispose();
    }
}