// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace fennecs;

public interface IRelationBacklink;

[StructLayout(LayoutKind.Explicit)]
internal readonly struct TypeId : IEquatable<TypeId>, IComparable<TypeId>
{
    //    This is a 64 bit union struct.
    //     Layout chart (little endian)
    // | LSB                          MSB |
    // | 64 bits                          |
    // | Value                            |
    // |-----------Equivalent-------------|
    // | 48 bits             |  16 bits   |
    // | Identity            | TypeNumber |
    
    [FieldOffset(0)] public readonly ulong Value;
    
    [FieldOffset(0)] public readonly Identity Target;
    
    [FieldOffset(6)] public readonly short TypeNumber;

    public bool isRelation => Target != Identity.None;
    public bool isBacklink => TypeNumber < 0;

    public bool Matches(TypeId other)
    {
        if (TypeNumber != other.TypeNumber) return false;

        // Most common case.
        if (Target == Identity.None) return other.Target == Identity.None;
        
        // Any only matches other Relations, not None.
        if (Target == Identity.Any) return other.Target != Identity.None;

        // Direct match.
        if (Target == other.Target) return true;
        
        // For commutative matching only. (usually a TypeId from a Query is matched against one from a Table)
        return other.Target == Identity.Any;
    } 

    public bool Equals(TypeId other) => Value == other.Value;

    public int CompareTo(TypeId other) => Value.CompareTo(other.Value);

    public override bool Equals(object? obj) => throw new InvalidCastException("Boxing Disallowed; use TypeId.Equals(TypeId) instead.");

    public static TypeId Create<T>(Identity target = default) 
    {
        var typeNumber = typeof(IRelationBacklink).IsAssignableFrom(typeof(T)) 
            ? (short) -LanguageTypeSource<T>.Id 
            : LanguageTypeSource<T>.Id;
        
        return new TypeId(target, typeNumber);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var low = (uint) (Value & 0xFFFFFFFFu);
            var high = (uint) (Value >> 32);
            return (int) (0x811C9DC5u * low + 0x1000193u * high + 0xc4ceb9fe1a85ec53u);
        }
    }


    public static bool operator ==(TypeId left, TypeId right)
    {
        return left.Equals(right);
    }


    public static bool operator !=(TypeId left, TypeId right)
    {
        return !(left == right);
    }

    public static implicit operator ulong(TypeId self) => self.Value;


    [SetsRequiredMembers]
    private TypeId(ulong value)
    {
        Value = value;
    }

    [SetsRequiredMembers]
    private TypeId(Identity target, short typeNumber)
    {
        Target = target;
        TypeNumber = typeNumber;
    }

    public static implicit operator TypeId(ulong other) => new(other);

    public override string ToString()
    {
        return $"{TypeNumber:x4}/{Target} == {Value:x16}#{GetHashCode()}";
    }
}

internal class TypeSource
{
    // ReSharper disable once StaticMemberInGenericType
    protected static short Counter;
}

// ReSharper disable once UnusedTypeParameter
// ReSharper disable once ClassNeverInstantiated.Global
internal class LanguageTypeSource<T> : TypeSource
{
    // ReSharper disable once StaticMemberInGenericType
    public static readonly short Id;

    static LanguageTypeSource()
    {
        if (Counter >= short.MaxValue) throw new InvalidOperationException("Language Level TypeIds exhausted.");
        Id = ++Counter;
    }
}

public readonly struct TypeExpression : IComparable<TypeExpression>, IEquatable<TypeExpression>
{
    public required Type Type { get; init; }
    public required ulong Value { get; init; }
    public required bool IsRelation { get; init; }

    public ushort TypeId => TypeIdConverter.Type(Value);

    public Identity Target => TypeIdConverter.Identity(Value);


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
        return IsRelation ? $"{GetHashCode()} {Type.Name}::{Target}" : $"{GetHashCode()} {Type.Name}";
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
        return TypeIdAssigner<T>.Id | (ulong) identity.Generation << 16 | (ulong) identity.Id << 32;
    }


    public static Identity Identity(ulong value)
    {
        return new Identity((int) (value >> 32), (ushort) (value >> 16));
    }


    public static ushort Type(ulong value)
    {
        return (ushort) value;
    }

    internal class TypeIdAssigner
    {
        protected static ushort Counter;
    }

    // ReSharper disable once UnusedTypeParameter
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class TypeIdAssigner<T> : TypeIdAssigner
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly ushort Id;

        static TypeIdAssigner()
        {
            if (Counter >= ushort.MaxValue)
            {
                throw new InvalidOperationException("TypeIds exhausted.");
            }

            Id = ++Counter;
        }
    }
}