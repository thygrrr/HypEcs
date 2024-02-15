// SPDX-License-Identifier: MIT

using System.Collections.Concurrent;

namespace fennecs;

public static class JobPool<T> where T : new()
{
    private static readonly ConcurrentBag<T> Pool = [];
    
    public static T Rent()
    {
        return Pool.TryTake(out var job) ? job : new T();
    }

    public static void Return(T job)
    {
        Pool.Add(job);
    }
    
    static JobPool()
    {
        for (var i = 0; i < 512; i++) Pool.Add(new T());
    }

    public static void Return(List<T> jobs)
    {
        foreach (var job in jobs) Return(job);
        jobs.Clear();
    }
}
