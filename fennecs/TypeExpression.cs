// SPDX-License-Identifier: MIT

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace fennecs;

[StructLayout(LayoutKind.Explicit)]
public readonly struct TypeExpression : IEquatable<TypeExpression>, IComparable<TypeExpression>
{
    //             This is a 64 bit union struct.
    //                 Layout: (little endian)
    //   | LSB                                   MSB |
    //   |-------------------------------------------|
    //   | Value                                     |
    //   | 64 bits                                   |
    //   |-------------------------------------------|
    //   | Id              | Generation | TypeNumber |
    //   | 32 bits         |  16 bits   |  16 bits   |
    //   |-------------------------------------------|
    //   | Identity                     | TypeNumber |
    //   | 48 bits                      |  16 bits   |
    
    //   PLANNED:
    //   TypeNumber
    //   | Type    | Flags |
    //   | 14 bits | 2 bits |
    
    //   Flags
    //   00 - Component Type
    //   01 - Component Type Targeting Entity
    //   10 - Component Type Targeting WeakReference
    //   11 - Reserved (for potential hash-bucket storage features)
    
    
    //Union Backing Store
    [FieldOffset(0)] public readonly ulong Value;

    //Identity Components
    [FieldOffset(0)] public readonly int Id;
    [FieldOffset(4)] public readonly ushort Generation;
    [FieldOffset(4)] public readonly TypeID Decoration;
    
    // Type Header
    [FieldOffset(6)] public readonly TypeID TypeId;

    public Entity Target => new(Value);

    public bool isRelation => TypeId != 0 && Target != Entity.None;

    public Type Type => LanguageType.Resolve(TypeId);
    
    /* TODO: Handle different flags if needed
        {
            return (TypeId, Id) switch
            {
                (0, int.MaxValue) => typeof(Any),
                (0, 0) => typeof(None),
                (0, _) => typeof(Entity),
                    _ => LanguageType.Resolve(TypeId),
            };
        }
        internal struct None;
        internal struct Any;    
    */

    public bool Matches(IEnumerable<TypeExpression> other)
    {
        var self = this;
        return other.Any(type => self.Matches(type));
    }
    
    public bool Matches(TypeExpression other)
    {
        // Reject if Type completely incompatible 
        if (TypeId != other.TypeId) return false;

        // Most common case.
        if (Target == Entity.None) return other.Target == Entity.None;
        
        // Any only matches other Relations, not pure components (Target == None).
        if (Target == Entity.Any) return other.Target != Entity.None;

        // Direct match.
        if (Target == other.Target) return true;
        
        // For commutative matching only. (usually a TypeId from a Query is matched against one from a Table)
        return other.Target == Entity.Any;
    } 

    public bool Equals(TypeExpression other) => Value == other.Value;

    public int CompareTo(TypeExpression other) => Value.CompareTo(other.Value);

    public override bool Equals(object? obj) => throw new InvalidCastException("Boxing Disallowed; use TypeId.Equals(TypeId) instead.");

    public static TypeExpression Create<T>(Entity target = default)
    {
        return new TypeExpression(target, LanguageType<T>.Id);
    }

    public static TypeExpression Create(Type type, Entity target = default)
    {
        return new TypeExpression(target, LanguageType.Identify(type));
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
    private TypeExpression(Entity target, TypeID typeId)
    {
        Value = target.Value;
        TypeId = typeId;
    }

    public override string ToString()
    {
        return isRelation ? $"<{LanguageType.Resolve(TypeId)}\u2192{Target}>" : $"<{LanguageType.Resolve(TypeId)}>";
    }
}