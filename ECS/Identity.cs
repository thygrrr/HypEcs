// SPDX-License-Identifier: MIT

namespace ECS;

public struct EntityMeta(Identity identity, int tableId, int row)
{
    public Identity Identity = identity;
    public int TableId = tableId;
    public int Row = row;
}

public readonly struct Identity(int id, ushort generation = 1) : IEquatable<Identity>
{
    public static Identity None = default;
    public static Identity Any = new(int.MaxValue, 0);

    public readonly int Id = id;
    public readonly ushort Generation = generation;

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
            var high = (uint) generation;
            return (int) (0x811C9DC5u * low + 0x1000193u * high + 0xc4ceb9fe1a85ec53u);
        }
    }

    public override string ToString()
    {
        return $"{Id}:{Generation}";
    }


    public static implicit operator Entity(Identity id) => new(id);
    public static bool operator ==(Identity left, Identity right) => left.Equals(right);
    public static bool operator !=(Identity left, Identity right) => !left.Equals(right);
}