using fennecs.pools;

namespace fennecs;

public readonly struct EntityBuilder(World world, Entity entity) : IDisposable
{
    private readonly PooledList<World.DeferredOperation> _operations = PooledList<World.DeferredOperation>.Rent();

/*
 TODO: Introduce to this pattern.
_operations.Add(
    new World.DeferredOperation()
    {
        Operation = World.Operation.Add,
        Identity = entity,
        Data = target,
    });
*/

    public void SyntaxTest()
    {
        Link(new Entity(new Identity(123)), "dieter");
        Link<int>(new Entity(new Identity(123)));
        Link(new Entity(new Identity(123)), 444);
    }
/*
    public EntityBuilder Add<T>(Entity target, T data) where T : class
    {
        data ??= new T();

        if (target == Identity.Any) throw new InvalidOperationException("EntityBuilder: Cannot relate to Identity.Any.");

        world.Link<T>(entity, target, data);

        return this;
    }
*/
    public EntityBuilder Link<T>(Entity target) where T : notnull, new()
    {
        world.Link(entity, target, new T());
        return this;
    }

    public EntityBuilder Link<T>(Entity target, T data)
    {
        world.Link(entity, target, data);
        return this;
    }

    public EntityBuilder Link<T>(T target) where T : class
    {
        world.Link(entity, target);
        return this;
    }

    [Obsolete("remove me")]
    public EntityBuilder Add<T>(Type type) where T : new()
    {
        world.AddComponent<T>(entity, new Identity(type));
        return this;
    }

    public EntityBuilder Add<T>(T data)
    {
        world.AddComponent(entity, data);
        return this;
    }


    public EntityBuilder Add<T>() where T : new()
    {
        world.AddComponent(entity, new T());
        return this;
    }

    /*
    public EntityBuilder Link<T>(Entity target, T data) 
    {
        if (target.Identity == Identity.Any) throw new InvalidOperationException("EntityBuilder: Cannot relate to Identity.Any.");
        
        world.AddComponent(entity, data, target);
        return this;
    }
    */

    [Obsolete("Remove me")]
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
        world.Unlink<T>(entity, target);
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