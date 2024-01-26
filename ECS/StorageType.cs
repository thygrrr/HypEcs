using System;

namespace ECS;

public readonly struct StorageType : IComparable<StorageType>, IEquatable<StorageType>
{
    public required Type Type { get; init; }
    public required ulong Value { get; init; }
    public required bool IsRelation { get; init; }

    public ushort TypeId
    {
        
        get => TypeIdConverter.Type(Value);
    }

    public Identity Identity
    {
        
        get => TypeIdConverter.Identity(Value);
    }

    
    public static StorageType Create<T>(Identity identity = default)
    {
        return new StorageType()
        {
            Value = TypeIdConverter.Value<T>(identity),
            Type = typeof(T),
            IsRelation = identity.Id > 0,
        };
    }

    
    public int CompareTo(StorageType other)
    {
        return Value.CompareTo(other.Value);
    }

    
    public override bool Equals(object? obj)
    {
        return (obj is StorageType other) && Value == other.Value;
    }
        
    
    public bool Equals(StorageType other)
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

    public static bool operator ==(StorageType left, StorageType right) => left.Equals(right);
    public static bool operator !=(StorageType left, StorageType right) => !left.Equals(right);

    public static implicit operator WildcardType(StorageType left)
    {
        return new WildcardType {Type = left.Type};
    }

    public static implicit operator Type(StorageType left)
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
        return (ushort)value;
    }

    class TypeIdAssigner
    {
        protected static ushort Counter;
    }

    // ReSharper disable once ClassNeverInstantiated.Local
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

public readonly struct WildcardType : IComparable<StorageType>, IEquatable<StorageType>, IComparable<WildcardType>, IEquatable<WildcardType>
{
    public required Type Type { get; init; }

    public int CompareTo(StorageType other)
    {
        return ReferenceEquals(other.Type, Type) ? 0 : -1;
    }

    public bool Equals(StorageType other)
    {
        return ReferenceEquals(other.Type, Type);
    }

    public int CompareTo(WildcardType other)
    {
        return ReferenceEquals(other.Type, Type) ? 0 : -1;
    }

    public bool Equals(WildcardType other)
    {
        return ReferenceEquals(other.Type, Type);
    }
}
