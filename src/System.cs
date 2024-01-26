using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace HypEcs;

public interface ISystem
{
    void Run(World world);
}

public sealed class SystemGroup : List<ISystem>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Run(World world)
    {
        foreach (var system in this)
        {
            system.Run(world);
        }
    }
}