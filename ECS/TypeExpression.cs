// SPDX-License-Identifier: MIT

namespace ECS;

public readonly struct TypeExpression : IComparable<TypeExpression>, IEquatable<TypeExpression>
{
    public required Type Type { get; init; }
    public required ulong Value { get; init; }
    public required bool IsRelation { get; init; }

    public ushort TypeId => TypeIdConverter.Type(Value);

    public Identity Identity => TypeIdConverter.Identity(Value);


    public static TypeExpression Create<T>(Identity identity = default) => new()
    {
        Type = typeof(T),
        Value = TypeIdConverter.Value<T>(identity),
        IsRelation = identity.Id > 0,
    };

    
    public int CompareTo(TypeExpression other)
    {
        return Value.CompareTo(other.Value);
    }

    
    public override bool Equals(object? obj)
    {
        return obj is TypeExpression other && Value == other.Value;
    }
        
    
    public bool Equals(TypeExpression other)
    {
        return Value == other.Value;
    }

    
    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    
    public override string ToString()
    {
        return IsRelation ? $"{GetHashCode()} {Type.Name}::{Identity}" : $"{GetHashCode()} {Type.Name}";
    }

    public static bool operator ==(TypeExpression left, TypeExpression right) => left.Equals(right);
    public static bool operator !=(TypeExpression left, TypeExpression right) => !left.Equals(right);

    public static implicit operator TypeFamily(TypeExpression left)
    {
        return new TypeFamily {Type = left.Type};
    }

    public static implicit operator Type(TypeExpression left)
    {
        return left.Type;
    }
}
    
public static class TypeIdConverter
{
    
    public static ulong Value<T>(Identity identity)
    {
        return TypeIdAssigner<T>.Id | (ulong)identity.Generation << 16 | (ulong)identity.Id << 32;
    }

    
    public static Identity Identity(ulong value)
    {
        return new Identity((int)(value >> 32), (ushort)(value >> 16));
    }

    
    public static ushort Type(ulong value)
    {
        return (ushort) value;
    }

    private class TypeIdAssigner
    {
        protected static ushort Counter;
    }

    // ReSharper disable once ClassNeverInstantiated.Local
    // ReSharper disable once UnusedTypeParameter
    private class TypeIdAssigner<T> : TypeIdAssigner
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly ushort Id;
        
        static TypeIdAssigner()
        {
            Id = ++Counter;
        }
    }
}