// SPDX-License-Identifier: MIT

namespace fennecs;

public readonly struct Identity(int id, ushort gen = 1) : IEquatable<Identity>
{
    public readonly int Id = id;
    public readonly ushort Generation = gen;
    
    public long Value => (uint) Id | (long) Generation << 32;
    
    public static readonly Identity None = new(0, 0);
    public static readonly Identity Any = new(int.MaxValue, ushort.MaxValue);
    
    public bool Equals(Identity other) => Id == other.Id && Generation == other.Generation;
    
    public override bool Equals(object? obj)
    {
        throw new InvalidCastException("Identity: Boxing equality comparisons disallowed. Use IEquatable<Identity>.Equals(Identity other) instead.");
        //return obj is Identity other && Equals(other); <-- second best option   
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var low = (uint) Id;
            var high = (uint) Generation;
            return (int) (0x811C9DC5u * low + 0x1000193u * high + 0xc4ceb9fe1a85ec53u);
        }
    }

    public override string ToString()
    {
        if (Equals(None)) return $"\u25c7none";
        if (Equals(Any)) return $"\u2bc1any";
        
        return $"\u2756{Id:x4}:{Generation:D5}";
    }


    public static implicit operator Entity(Identity id) => new(id);
    public static bool operator ==(Identity left, Identity right) => left.Equals(right);
    public static bool operator !=(Identity left, Identity right) => !left.Equals(right);
}