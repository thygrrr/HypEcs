using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ECS;

public static class MaskPool
{
    static readonly Stack<Mask> Stack = new();

    
    public static Mask Get()
    {
        return Stack.Count > 0 ? Stack.Pop() : new Mask();
    }

    
    public static void Add(Mask list)
    {
        list.Clear();
        Stack.Push(list);
    }
}