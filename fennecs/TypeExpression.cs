// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace fennecs;

[StructLayout(LayoutKind.Explicit)]
public readonly struct TypeExpression : IEquatable<TypeExpression>, IComparable<TypeExpression>
{
    //    This is a 128 bit union struct.
    //     Layout chart (little endian)
    // | LSB                                       MSB |
    // | 64 bits              |                64 bits |
    // | Value                |                   Type |
    // |-----------------------------------------------|
    // | 48 bits       |  16 bits   |          64 bits |
    // | Identity      | TypeNumber |             Type |
    
    [FieldOffset(0)] public readonly ulong Value;
    
    [FieldOffset(0)] public readonly Identity Target;

    [FieldOffset(6)] public readonly short TypeId;

    [FieldOffset(8)] public readonly Type Type;

    public bool isRelation => Target != Identity.None;
    public bool isBacklink => TypeId < 0;


    public bool Matches(TypeExpression other)
    {
        if (TypeId != other.TypeId) return false;

        // Most common case.
        if (Target == Identity.None) return other.Target == Identity.None;
        
        // Any only matches other Relations, not None.
        if (Target == Identity.Any) return other.Target != Identity.None;

        // Direct match.
        if (Target == other.Target) return true;
        
        // For commutative matching only. (usually a TypeId from a Query is matched against one from a Table)
        return other.Target == Identity.Any;
    } 

    public bool Equals(TypeExpression other) => Value == other.Value;

    public int CompareTo(TypeExpression other) => Value.CompareTo(other.Value);

    public override bool Equals(object? obj) => throw new InvalidCastException("Boxing Disallowed; use TypeId.Equals(TypeId) instead.");

    public static TypeExpression Create<T>(Identity target = default) 
    {
        var key = typeof(IRelationBacklink).IsAssignableFrom(typeof(T)) 
            ? (short) -LanguageTypeSource<T>.Id 
            : LanguageTypeSource<T>.Id;
        
        return new TypeExpression(target, key, typeof(T));
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


    public static bool operator ==(TypeExpression left, TypeExpression right)
    {
        return left.Equals(right);
    }


    public static bool operator !=(TypeExpression left, TypeExpression right)
    {
        return !(left == right);
    }

    public static implicit operator ulong(TypeExpression self) => self.Value;


    [SetsRequiredMembers]
    private TypeExpression(ulong value, Type type)
    {
        Value = value;
        Type = type;
    }

    [SetsRequiredMembers]
    private TypeExpression(Identity target, short typeId, Type type)
    {
        Target = target;
        TypeId = typeId;
        Type = type;
    }

    public override string ToString()
    {
        return $"{TypeId:x4}/{Target} == {Value:x16}#{GetHashCode()}";
    }
}

public interface IRelationBacklink;

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
