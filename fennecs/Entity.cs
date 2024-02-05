// SPDX-License-Identifier: MIT

namespace fennecs;

public readonly struct Entity(Identity identity)
{
    public static readonly Entity None = default;
    public static readonly Entity Any = new(Identity.Any);

    public bool IsAny => Identity == Identity.Any;
    public bool IsNone => Identity == default;

    public Identity Identity { get; } = identity;


    public override bool Equals(object? obj)
    {
        return obj is Entity entity && Identity.Equals(entity.Identity);
    }

    
    public override int GetHashCode()
    {
        return Identity.GetHashCode();
    }

    
    public override string ToString()
    {
        return $"🧩{Identity}";
    }

    
    public static bool operator ==(Entity left, Entity right) => left.Equals(right);

    
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}

public readonly struct EntityBuilder(World world, Entity entity)
{
    public EntityBuilder Add<T>(Entity target = default) where T : struct
    {
        if (target.Identity == Identity.Any) throw new InvalidOperationException("EntityBuilder: Cannot relate to Identity.Any.");
        world.AddComponent<T>(entity, target);
        return this;
    }

    /*
     I don't like these semantics, but they could be useful.
     However, this strongly burdens the TypeExpression space.
     
    public EntityBuilder Add<T>(Type type) where T : struct
    {
        var typeEntity = World.GetTypeEntity(type);
        World.AddComponent<T>(_entity, typeEntity);
        return this;
    }
    */

    
    public EntityBuilder Add<T>(T data) where T : struct
    {
        world.AddComponent(entity, data);
        return this;
    }

    
    public EntityBuilder Add<T>(T data, Entity target) where T : struct
    {
        if (target.Identity == Identity.Any) throw new InvalidOperationException("EntityBuilder: Cannot relate to Identity.Any.");
        
        world.AddComponent(entity, data, target);
        return this;
    }

    
    public EntityBuilder Add<T>(T data, Type type) where T : struct
    {
        var typeEntity = world.GetTypeEntity(type);
        world.AddComponent(entity, data, typeEntity);
        return this;
    }

    
    public EntityBuilder Remove<T>() where T : struct
    {
        world.RemoveComponent<T>(entity);
        return this;
    }

    
    public EntityBuilder Remove<T>(Entity target) where T : struct
    {
        world.RemoveComponent<T>(entity, target);
        return this;
    }

    
    public EntityBuilder Remove<T>(Type type) where T : struct
    {
        var typeEntity = world.GetTypeEntity(type);
        world.RemoveComponent<T>(entity, typeEntity);
        return this;
    }

    public Entity Id()
    {
        return entity;
    }
}