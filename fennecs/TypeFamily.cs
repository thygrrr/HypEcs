// SPDX-License-Identifier: MIT

namespace fennecs;

public readonly struct TypeFamily : 
    IComparable<TypeExpression>, 
    IEquatable<TypeExpression>, 
    IComparable<TypeFamily>, 
    IEquatable<TypeFamily>
{
    public required Type Type { get; init; }

    public int CompareTo(TypeExpression other)
    {
        return GetHashCode().CompareTo(other.Type.GetHashCode());
    }

    public bool Equals(TypeExpression other)
    {
        return other.Type == Type;
    }

    public int CompareTo(TypeFamily other)
    {
        return GetHashCode().CompareTo(other.GetHashCode());
    }

    public bool Equals(TypeFamily other)
    {
        return other.Type == Type;
    }

    public override bool Equals(object? obj)
    {
        return obj is TypeFamily family && Equals(family);
    }

    public override int GetHashCode()
    {
        return Type.GetHashCode();
    }

    public static bool operator ==(TypeFamily left, TypeFamily right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(TypeFamily left, TypeFamily right)
    {
        return !(left == right);
    }
}