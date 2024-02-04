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

    //TODO: May be unnecessary
    [Obsolete("Boxing equality comparisons removed.")]
    public override bool Equals(object? obj) => obj is Identity other && Id == other.Id && Generation == other.Generation;
    
    public override int GetHashCode()
    {
        unchecked // Allow arithmetic overflow, numbers will just "wrap around"
        {
            var hashcode = 1430287;
            hashcode = hashcode * 7302013 ^ Id.GetHashCode();
            hashcode = hashcode * 7302013 ^ Generation.GetHashCode();
            return hashcode;
        }
    }

    
    public override string ToString()
    {
        return $"{id}:{generation}";
    }


    public static implicit operator Entity(Identity id) => new(id);
    public static bool operator ==(Identity left, Identity right) => left.Equals(right);
    public static bool operator !=(Identity left, Identity right) => !left.Equals(right);
}