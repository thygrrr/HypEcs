// SPDX-License-Identifier: MIT

namespace fennecs;

public readonly struct Entity(Identity identity) : IComparable<Entity>
{
    public static readonly Entity None = default;
    public static readonly Entity Any = new(Identity.Any);

    internal Identity Identity { get; } = identity;

    public bool IsType => Identity.IsObject;

    public int CompareTo(Entity other)
    {
        return Identity.CompareTo(other.Identity);
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
        return Identity.ToString();
    }

    public static implicit operator Identity(Entity left) => left.Identity;
    
    public static bool operator ==(Entity left, Entity right) => left.Equals(right);

    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

    public static bool operator ==(Identity left, Entity right) => left.Equals(right.Identity);

    public static bool operator !=(Identity left, Entity right) => !left.Equals(right);

    public static bool operator ==(Entity left, Identity right) => left.Identity.Equals(right);

    public static bool operator !=(Entity left, Identity right) => !left.Identity.Equals(right);
}