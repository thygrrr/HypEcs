namespace ECS;

public struct EntityMeta
{
    public Identity Identity;
    public int TableId;
    public int Row;

    public EntityMeta(Identity identity, int tableId, int row)
    {
        Identity = identity;
        TableId = tableId;
        Row = row;
    }
}

public readonly struct Identity
{
    public static Identity None = default;
    public static Identity Any = new(int.MaxValue, 0);
    
    public readonly int Id;
    public readonly ushort Generation;

    
    public Identity(int id, ushort generation = 1)
    {
        Id = id;
        Generation = generation;
    }

    
    public override bool Equals(object? obj)
    {
        return (obj is Identity other) && Id == other.Id && Generation == other.Generation;
    }

    
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
        return Id.ToString();
    }


    public static implicit operator Entity(Identity id) => new(id);
    public static bool operator ==(Identity left, Identity right) => left.Equals(right);
    public static bool operator !=(Identity left, Identity right) => !left.Equals(right);
}