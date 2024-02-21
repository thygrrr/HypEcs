// SPDX-License-Identifier: MIT

namespace fennecs;

public readonly struct Entity(Identity identity) : IComparable<Entity>
{
    internal readonly Identity Identity = identity;

    public int CompareTo(Entity other) => Identity.CompareTo(other.Identity);

    public override bool Equals(object? obj)
    {
        return obj is Entity entity && Identity.Equals(entity.Identity);
    }

    public override int GetHashCode() => Identity.GetHashCode();

    public override string ToString() => Identity.ToString();

    public static implicit operator Identity(Entity left) => left.Identity;

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);

    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);
}
