using fennecs.pools;

namespace fennecs;

public readonly struct EntityBuilder(World world, Identity identity) : IDisposable
{
    private readonly PooledList<World.DeferredOperation> _operations = PooledList<World.DeferredOperation>.Rent();

/*
 TODO: Introduce to this pattern.
_operations.Add(
    new World.DeferredOperation()
    {
        Operation = World.Operation.Add,
        IdIdentity = identity,
        Data = target,
    });
*/
    public EntityBuilder Link<T>(Identity target) where T : notnull, new()
    {
        world.Link(identity, target, new T());
        return this;
    }

    public EntityBuilder Link<T>(Identity target, T data)
    {
        world.Link(identity, target, data);
        return this;
    }

    public EntityBuilder Link<T>(T target) where T : class
    {
        world.Link(identity, target);
        return this;
    }

    public EntityBuilder Add<T>(T data)
    {
        world.AddComponent(identity, data);
        return this;
    }


    public EntityBuilder Add<T>() where T : new()
    {
        world.AddComponent(identity, new T());
        return this;
    }

    
    public EntityBuilder Remove<T>() 
    {
        world.RemoveComponent<T>(identity);
        return this;
    }
    
    public EntityBuilder Remove<T>(Identity target) 
    {
        world.Unlink<T>(identity, target);
        return this;
    }
    
    public Identity Id()
    {
        Dispose();
        return identity;
    }

    public void Dispose()
    {
        _operations.Dispose();
    }
}