// SPDX-License-Identifier: MIT

namespace ECS;

public static class MaskPool
{
    private static readonly Stack<Mask> Stack = new();

    
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