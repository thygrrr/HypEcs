using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ECS;

public static class ListPool<T>
{
    private static readonly Stack<List<T>> Stack = new();

    
    public static List<T> Get()
    {
        return Stack.TryPop(out var list) ? list : new List<T>();
    }

    
    public static void Add(List<T> list)
    {
        list.Clear();
        Stack.Push(list);
    }
}