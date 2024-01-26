using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ECS;

public sealed class Mask
{
    internal readonly List<StorageType> HasTypes = new();
    internal readonly List<StorageType> NotTypes = new();
    internal readonly List<StorageType> AnyTypes = new();

    
    public void Has(StorageType type)
    {
        HasTypes.Add(type);
    }

    
    public void Not(StorageType type)
    {
        NotTypes.Add(type);
    }

    
    public void Any(StorageType type)
    {
        AnyTypes.Add(type);
    }

    public void Clear()
    {
        HasTypes.Clear();
        NotTypes.Clear();
        AnyTypes.Clear();
    }

    
    public override int GetHashCode()
    {
        var hash = HasTypes.Count + AnyTypes.Count + NotTypes.Count;

        unchecked
        {
            foreach (var type in HasTypes) hash = hash * 314159 + type.Value.GetHashCode();
            foreach (var type in NotTypes) hash = hash * 314159 - type.Value.GetHashCode();
            foreach (var type in AnyTypes) hash *= 314159 * type.Value.GetHashCode();
        }

        return hash;
    }
}