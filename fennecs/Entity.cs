// SPDX-License-Identifier: MIT

using System.Runtime.InteropServices;

namespace fennecs;

/// <summary>
/// Refers to an entity, object, or virtual concept (e.g. any/none wildcard).
/// </summary>
/// <param name="value"></param>
[StructLayout(LayoutKind.Explicit)]
public readonly struct Entity(ulong value) : IEquatable<Entity>, IComparable<Entity>
{
    [FieldOffset(0)] internal readonly ulong Value = value;
    [FieldOffset(0)] internal readonly int Id;
    
    [FieldOffset(4)] internal readonly ushort Generation;
    [FieldOffset(4)] internal readonly TypeID Decoration;

    [FieldOffset(6)] internal readonly TypeID RESERVED = 0;

    [FieldOffset(0)] internal readonly uint DWordLow;
    [FieldOffset(4)] internal readonly uint DWordHigh;
    
    //public ulong Value => (uint) Id | (ulong) Generation << 32;

    public static readonly Entity None = new(0, 0);
    public static readonly Entity Any = new(0, TypeID.MaxValue);
    
    // Entity Reference.
    public bool IsReal => Id > 0 && Generation > 0;

    // Tracked Object Reference.
    public bool IsObject => Decoration < 0;

    // Special Entities, such as None, Any.
    public bool IsVirtual => Decoration >= 0 && Id <= 0;

    public static implicit operator Entity(Type type) => new(type);

    public Entity(int id, TypeID decoration = 1) : this((uint) id | (ulong) decoration << 32)
    {
    }
    
    internal Entity(TypeID typeId) : this(0, typeId)
    {
    }

    public Entity(Type type) : this(LanguageType.Identify(type))
    {
    }

    public static Entity Of<T>(T item) where T : class
    {
        return new(item.GetHashCode(), LanguageType<T>.TargetId);
    }
    
    public bool Equals(Entity other) => Id == other.Id && Generation == other.Generation;

    public int CompareTo(Entity other)
    {
        return Value.CompareTo(other.Value);
    }

    public override bool Equals(object? obj)
    {
        throw new InvalidCastException("Identity: Boxing equality comparisons disallowed. Use IEquatable<Identity>.Equals(Identity other) instead.");
        //return obj is Identity other && Equals(other); <-- second best option   
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (int) (0x811C9DC5u * DWordLow + 0x1000193u * DWordHigh + 0xc4ceb9fe1a85ec53u);
        }
    }

    public static bool operator ==(Entity left, Entity right) => left.Equals(right);
    public static bool operator !=(Entity left, Entity right) => !left.Equals(right);

    public Type Type => Id switch
    {
        <= 0 => LanguageType.Resolve(Decoration),
        _ => typeof(Entity),
    };

    public Entity Successor
    {
        get
        {
            if (!IsReal) throw new InvalidCastException("Cannot reuse virtual Identities");
                
            var generationWrappedStartingAtOne = (TypeID) (Generation % (TypeID.MaxValue - 1) + 1);
            return new Entity(Id, generationWrappedStartingAtOne);
        }
    }

    public override string ToString()
    {
        if (this == None)
            return $"None";

        if (this == Any)
            return $"Any";

        return IsObject ? $"{Type}" : $"\u2756{Id:x8}:{Generation:D5}";
    }
}