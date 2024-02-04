// SPDX-License-Identifier: MIT

namespace ECS;

public readonly struct Entity
{
    public static readonly Entity None = default;
    public static readonly Entity Any = new(Identity.Any);

    public bool IsAny => Identity == Identity.Any;
    public bool IsNone => Identity == default;

    public Identity Identity { get; }

    public Entity(Identity identity)
    {
        Identity = identity;
    }

    
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

public readonly struct EntityBuilder
{
    internal readonly World World;
    private readonly Entity _entity;

    
    public EntityBuilder(World world, Entity entity)
    {
        World = world;
        _entity = entity;
    }

    
    public EntityBuilder Add<T>(Entity target = default) where T : struct
    {
        World.AddComponent<T>(_entity, target);
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
        World.AddComponent(_entity, data);
        return this;
    }

    
    public EntityBuilder Add<T>(T data, Entity target) where T : struct
    {
        World.AddComponent(_entity, data, target);
        return this;
    }

    
    public EntityBuilder Add<T>(T data, Type type) where T : struct
    {
        var typeEntity = World.GetTypeEntity(type);
        World.AddComponent(_entity, data, typeEntity);
        return this;
    }

    
    public EntityBuilder Remove<T>() where T : struct
    {
        World.RemoveComponent<T>(_entity);
        return this;
    }

    
    public EntityBuilder Remove<T>(Entity target) where T : struct
    {
        World.RemoveComponent<T>(_entity, target);
        return this;
    }

    
    public EntityBuilder Remove<T>(Type type) where T : struct
    {
        var typeEntity = World.GetTypeEntity(type);
        World.RemoveComponent<T>(_entity, typeEntity);
        return this;
    }

    public Entity Id()
    {
        return _entity;
    }
}