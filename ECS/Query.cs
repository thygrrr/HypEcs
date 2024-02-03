// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

using System.Buffers;
using System.Threading.Channels;

namespace ECS;

public class Query(Archetypes archetypes, Mask mask, List<Table> tables)
{
    private protected readonly List<Table> Tables = tables;
    private protected readonly Archetypes Archetypes = archetypes;
    protected internal readonly Mask Mask = mask;

    public bool Has(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        return Tables.Contains(table);
    }

    
    internal void AddTable(Table table)
    {
        Tables.Add(table);
    }
}

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo
public delegate void QueryAction_C<C0>(ref C0 comp0);

public delegate void QueryAction_CC<C0, C1>(ref C0 comp0, ref C1 comp1);

public delegate void QueryAction_CCC<C0, C1, C2>(ref C0 comp0, ref C1 comp1, ref C2 comp2);

public delegate void QueryAction_CCCC<C0, C1, C2, C3>(ref C0 c0, ref C1 c1, ref C2 c2, ref C3 c3);

public delegate void QueryAction_CCCCC<C0, C1, C2, C3, C4>(ref C0 c0, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4);


public delegate void QueryAction_CS<C0>(ref C0 comp0, ParallelLoopState state);

public delegate void QueryAction_CCS<C0, C1>(ref C0 comp0, ref C1 comp1, ParallelLoopState state);

public delegate void QueryAction_CCCS<C0, C1, C2>(ref C0 comp0, ref C1 comp1, ref C2 comp2, ParallelLoopState state);

public delegate void QueryAction_CCCCS<C0, C1, C2, C3>(ref C0 c0, ref C1 c1, ref C2 c2, ref C3 c3, ParallelLoopState state);

public delegate void QueryAction_CCCCCS<C0, C1, C2, C3, C4>(ref C0 c0, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, ParallelLoopState state);


public delegate void QueryAction_CU<C0, in U>(ref C0 comp0, U uniform);

public delegate void QueryAction_CCU<C0, C1, in U>(ref C0 comp0, ref C1 comp1, U uniform);

public delegate void QueryAction_CCCU<C0, C1, C2, in U>(ref C0 comp0, ref C1 comp1, ref C2 comp2, U uniform);

public delegate void QueryAction_CCCCU<C0, C1, C2, C3, in U>(ref C0 c0, ref C1 c1, ref C2 c2, ref C3 c3, U uniform);

public delegate void QueryAction_CCCCCU<C0, C1, C2, C3, C4, in U>(ref C0 c0, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, U uniform);


public delegate void SpanAction_C<C0>(Span<C0> comp0);

public delegate void SpanAction_CC<C0, C1>(Span<C0> comp0, Span<C1> comp1);

public delegate void SpanAction_CCC<C0, C1, C2>(Span<C0> comp0, ref C1 comp1, ref C2 comp2);

public delegate void SpanAction_CCCC<C0, C1, C2, C3>(Span<C0> c0, Span<C1> c1, Span<C2> c2, Span<C3> c3);

public delegate void SpanAction_CCCCC<C0, C1, C2, C3, C4>(Span<C0> c0, Span<C1> c1, Span<C2> c2, Span<C3> c3, Span<C4> c4);


/* TODO: These would be used for "early out" and search type algorithms.
public delegate void QueryAction_CUS<C0, in U>(ref C0 comp0, U uniform, ParallelLoopState state);

public delegate void QueryAction_CCUS<C0, C1, in U>(ref C0 comp0, ref C1 comp1, U uniform, ParallelLoopState state);

public delegate void QueryAction_CCCUS<C0, C1, C2, in U>(ref C0 comp0, ref C1 comp1, ref C2 comp2, U uniform, ParallelLoopState state);

public delegate void QueryAction_CCCCUS<C0, C1, C2, C3, in U>(ref C0 c0, ref C1 c1, ref C2 c2, ref C3 c3, U uniform, ParallelLoopState state);

public delegate void QueryAction_CCCCCUS<C0, C1, C2, C3, C4, in U>(ref C0 c0, ref C1 c1, ref C2 c2, ref C3 c3, ref C4 c4, U uniform, ParallelLoopState state);
*/

// ReSharper enable IdentifierTypo
// ReSharper enable InconsistentNaming

public class Query<C>(Archetypes archetypes, Mask mask, List<Table> tables) : Query(archetypes, mask, tables)
    where C : struct
{
    private const int SPIN_TIMEOUT = 420; // ~10 microseconds
    private readonly ParallelOptions opts = new() {MaxDegreeOfParallelism = 16};

    public ref C Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var storage = table.GetStorage<C>(Identity.None);
        return ref storage[meta.Row];
    }

    #region Runners
    public void Run(QueryAction_C<C> action)
    {
        Archetypes.Lock();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None).AsSpan();
            foreach (ref var c in storage) action(ref c);
        }

        Archetypes.Unlock();
    }

    public void RunParallel(QueryAction_C<C> action, int chunkSize = int.MaxValue)
    {
        Archetypes.Lock();

        var queued = 0;

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None);
            var length = table.Count;

            var partitions = Math.Clamp(length / chunkSize, 1, opts.MaxDegreeOfParallelism);
            var partitionSize = length / partitions;

            for (var partition = 1; partition < partitions; partition++)
            {
                Interlocked.Increment(ref queued);

                ThreadPool.QueueUserWorkItem(delegate(int part)
                {
                    foreach (ref var c in storage.AsSpan(part * partitionSize, partitionSize))
                    {
                        action(ref c);
                    }

                    // ReSharper disable once AccessToModifiedClosure
                    Interlocked.Decrement(ref queued);
                }, partition, preferLocal: true);
            }

            //Optimization: Also process one partition right here on the calling thread.
            foreach (ref var c in storage.AsSpan(0, partitionSize))
            {
                action(ref c);
            }
        }
        
        while (queued > 0) Thread.SpinWait(SPIN_TIMEOUT);
        Archetypes.Unlock();
    }

 

    public void Run<U>(QueryAction_CU<C, U> action, U uniform)
    {
        Archetypes.Lock();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None).AsSpan(0, table.Count);
            foreach (ref var c in storage) action(ref c, uniform);
        }

        Archetypes.Unlock();
    }


    public void RunParallel<U>(QueryAction_CU<C, U> action, U uniform, int chunkSize = int.MaxValue)
    {
        Archetypes.Lock();
        var queued = 0;

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None);

            var length = table.Count;
            
            var partitions = Math.Clamp(length / chunkSize, 1, opts.MaxDegreeOfParallelism);
            var partitionSize = length / partitions;

            for (var partition = 1; partition < partitions; partition++)
            {
                Interlocked.Increment(ref queued);

                ThreadPool.QueueUserWorkItem(delegate(int part)
                {
                    foreach (ref var c in storage.AsSpan(part * partitionSize, partitionSize))
                    {
                        action(ref c, uniform);
                    }

                    // ReSharper disable once AccessToModifiedClosure
                    Interlocked.Decrement(ref queued);
                }, partition, preferLocal: true);
            }

            //Optimization: Also process one partition right here on the calling thread.
            foreach (ref var c in storage.AsSpan(0, partitionSize))
            {
                action(ref c, uniform);
            }
        }

        while (queued > 0) Thread.Yield();
        Archetypes.Unlock();
    }


    public void Run(SpanAction_C<C> action)
    {
        Archetypes.Lock();
        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            action(table.GetStorage<C>(Identity.None).AsSpan(0, table.Count));
        }

        Archetypes.Unlock();
    }
    
    public void Raw(Action<Memory<C>> action)
    {
        Archetypes.Lock();
        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            action(table.GetStorage<C>(Identity.None).AsMemory(0, table.Count));
        }
        Archetypes.Unlock();
    }
    #endregion
}

public class Query<C1, C2>(Archetypes archetypes, Mask mask, List<Table> tables) : Query(archetypes, mask, tables)
    where C1 : struct
    where C2 : struct
{
    public RefValueTuple<C1, C2> Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var storage1 = table.GetStorage<C1>(Identity.None);
        var storage2 = table.GetStorage<C2>(Identity.None);
        return new RefValueTuple<C1, C2>(ref storage1[meta.Row], ref storage2[meta.Row]);
    }

    #region Runners

    public void Run(QueryAction_CC<C1, C2> action)
    {
        Archetypes.Lock();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage1 = table.GetStorage<C1>(Identity.None).AsSpan();
            var storage2 = table.GetStorage<C2>(Identity.None).AsSpan();
            for (var i = 0; i < storage1.Length; i++)
            {
                action(ref storage1[i], ref storage2[i]);
            }
        }

        Archetypes.Unlock();
    }


    public void Run<U>(QueryAction_CCU<C1, C2, U> action, U uniform)
    {
        Archetypes.Lock();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage1 = table.GetStorage<C1>(Identity.None).AsSpan();
            var storage2 = table.GetStorage<C2>(Identity.None).AsSpan();
            for (var i = 0; i < storage1.Length; i++)
            {
                action(ref storage1[i], ref storage2[i], uniform);
            }
        }

        Archetypes.Unlock();
    }


    public void RunParallel(QueryAction_CC<C1, C2> action)
    {
        Archetypes.Lock();

        Parallel.ForEach(Tables, delegate(Table table)
        {
            if (table.IsEmpty) return;
            var storage1 = table.GetStorage<C1>(Identity.None).AsSpan();
            var storage2 = table.GetStorage<C2>(Identity.None).AsSpan();
            for (var i = 0; i < storage1.Length; i++)
            {
                action(ref storage1[i], ref storage2[i]);
            }
        });

        Archetypes.Unlock();
    }


    public void RunParallel<U>(QueryAction_CCU<C1, C2, U> action, U uniform)
    {
        Archetypes.Lock();

        Parallel.ForEach(Tables, delegate(Table table)
        {
            if (table.IsEmpty) return;
            var storage1 = table.GetStorage<C1>(Identity.None).AsSpan();
            var storage2 = table.GetStorage<C2>(Identity.None).AsSpan();
            for (var i = 0; i < storage1.Length; i++)
            {
                action(ref storage1[i], ref storage2[i], uniform);
            }
        });

        Archetypes.Unlock();
    }

    #endregion
}

public class Query<C1, C2, C3>(Archetypes archetypes, Mask mask, List<Table> tables) : Query(archetypes, mask, tables)
    where C1 : struct
    where C2 : struct
    where C3 : struct
{
    public RefValueTuple<C1, C2, C3> Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var storage1 = table.GetStorage<C1>(Identity.None);
        var storage2 = table.GetStorage<C2>(Identity.None);
        var storage3 = table.GetStorage<C3>(Identity.None);
        return new RefValueTuple<C1, C2, C3>(ref storage1[meta.Row], ref storage2[meta.Row],
            ref storage3[meta.Row]);
    }

    public void Run(Action<int, C1[], C2[], C3[]> action)
    {
        Archetypes.Lock();
        
        for (var t = 0; t < Tables.Count; t++)
        {
            var table = Tables[t];

            if (table.IsEmpty) continue;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);

            action(table.Count, s1, s2, s3);
        }
        
        Archetypes.Unlock();
    }
    
    public void RunParallel(Action<int, C1[], C2[], C3[]> action)
    {
        Archetypes.Lock();

        Parallel.For(0, Tables.Count, t =>
        {
            var table = Tables[t];
            
            if (table.IsEmpty) return;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            
            action(table.Count, s1, s2, s3);
        });
        
        Archetypes.Unlock();
    }
}

public class Query<C1, C2, C3, C4>(Archetypes archetypes, Mask mask, List<Table> tables) : Query(archetypes, mask, tables)
    where C1 : struct
    where C2 : struct
    where C3 : struct
    where C4 : struct
{
    public RefValueTuple<C1, C2, C3, C4> Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var s1 = table.GetStorage<C1>(Identity.None);
        var s2 = table.GetStorage<C2>(Identity.None);
        var s3 = table.GetStorage<C3>(Identity.None);
        var s4 = table.GetStorage<C4>(Identity.None);
        return new RefValueTuple<C1, C2, C3, C4>(ref s1[meta.Row], ref s2[meta.Row],
            ref s3[meta.Row], ref s4[meta.Row]);
    }

    public void Run(Action<int, C1[], C2[], C3[], C4[]> action)
    {
        Archetypes.Lock();

        for (var t = 0; t < Tables.Count; t++)
        {
            var table = Tables[t];

            if (table.IsEmpty) continue;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);

            action(table.Count, s1, s2, s3, s4);
        }

        Archetypes.Unlock();
    }

    public void RunParallel(Action<int, C1[], C2[], C3[], C4[]> action)
    {
        Archetypes.Lock();

        Parallel.For(0, Tables.Count, t =>
        {
            var table = Tables[t];
            
            if (table.IsEmpty) return;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            
            action(table.Count, s1, s2, s3, s4);
        });
        
        Archetypes.Unlock();
    }
}

public class Query<C1, C2, C3, C4, C5>(Archetypes archetypes, Mask mask, List<Table> tables) : Query(archetypes, mask, tables)
    where C1 : struct
    where C2 : struct
    where C3 : struct
    where C4 : struct
    where C5 : struct
{
    public RefValueTuple<C1, C2, C3, C4, C5> Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var s1 = table.GetStorage<C1>(Identity.None);
        var s2 = table.GetStorage<C2>(Identity.None);
        var s3 = table.GetStorage<C3>(Identity.None);
        var s4 = table.GetStorage<C4>(Identity.None);
        var s5 = table.GetStorage<C5>(Identity.None);
        return new RefValueTuple<C1, C2, C3, C4, C5>(ref s1[meta.Row], ref s2[meta.Row],
            ref s3[meta.Row], ref s4[meta.Row], ref s5[meta.Row]);
    }

    public void Run(Action<int, C1[], C2[], C3[], C4[], C5[]> action)
    {
        Archetypes.Lock();
        
        for (var t = 0; t < Tables.Count; t++)
        {
            var table = Tables[t];

            if (table.IsEmpty) continue;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);

            action(table.Count, s1, s2, s3, s4, s5);
        }
        
        Archetypes.Unlock();
    }
    
    public void RunParallel(Action<int, C1[], C2[], C3[], C4[], C5[]> action)
    {
        Archetypes.Lock();

        Parallel.For(0, Tables.Count, t =>
        {
            var table = Tables[t];
            
            if (table.IsEmpty) return;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);
            
            action(table.Count, s1, s2, s3, s4, s5);
        });
        
        Archetypes.Unlock();
    }
}

public class Query<C1, C2, C3, C4, C5, C6>(Archetypes archetypes, Mask mask, List<Table> tables) : Query(archetypes, mask, tables)
    where C1 : struct
    where C2 : struct
    where C3 : struct
    where C4 : struct
    where C5 : struct
    where C6 : struct
{
    public RefValueTuple<C1, C2, C3, C4, C5, C6> Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var s1 = table.GetStorage<C1>(Identity.None);
        var s2 = table.GetStorage<C2>(Identity.None);
        var s3 = table.GetStorage<C3>(Identity.None);
        var s4 = table.GetStorage<C4>(Identity.None);
        var s5 = table.GetStorage<C5>(Identity.None);
        var s6 = table.GetStorage<C6>(Identity.None);
        return new RefValueTuple<C1, C2, C3, C4, C5, C6>(ref s1[meta.Row], ref s2[meta.Row],
            ref s3[meta.Row], ref s4[meta.Row], ref s5[meta.Row],
            ref s6[meta.Row]);
    }

    public void Run(Action<int, C1[], C2[], C3[], C4[], C5[], C6[]> action)
    {
        Archetypes.Lock();
        
        for (var t = 0; t < Tables.Count; t++)
        {
            var table = Tables[t];

            if (table.IsEmpty) continue;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);
            var s6 = table.GetStorage<C6>(Identity.None);

            action(table.Count, s1, s2, s3, s4, s5, s6);
        }
        
        Archetypes.Unlock();
    }
    
    public void RunParallel(Action<int, C1[], C2[], C3[], C4[], C5[], C6[]> action)
    {
        Archetypes.Lock();

        Parallel.For(0, Tables.Count, t =>
        {
            var table = Tables[t];
            
            if (table.IsEmpty) return;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);
            var s6 = table.GetStorage<C6>(Identity.None);
            
            action(table.Count, s1, s2, s3, s4, s5, s6);
        });
        
        Archetypes.Unlock();
    }
}

public class Query<C1, C2, C3, C4, C5, C6, C7> : Query
    where C1 : struct
    where C2 : struct
    where C3 : struct
    where C4 : struct
    where C5 : struct
    where C6 : struct
    where C7 : struct
{
    public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
    {
    }

    
    public RefValueTuple<C1, C2, C3, C4, C5, C6, C7> Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var s1 = table.GetStorage<C1>(Identity.None);
        var s2 = table.GetStorage<C2>(Identity.None);
        var s3 = table.GetStorage<C3>(Identity.None);
        var s4 = table.GetStorage<C4>(Identity.None);
        var s5 = table.GetStorage<C5>(Identity.None);
        var s6 = table.GetStorage<C6>(Identity.None);
        var s7 = table.GetStorage<C7>(Identity.None);
        return new RefValueTuple<C1, C2, C3, C4, C5, C6, C7>(ref s1[meta.Row], ref s2[meta.Row],
            ref s3[meta.Row], ref s4[meta.Row], ref s5[meta.Row],
            ref s6[meta.Row], ref s7[meta.Row]);
    }

    public void Run(Action<int, C1[], C2[], C3[], C4[], C5[], C6[], C7[]> action)
    {
        Archetypes.Lock();
        
        for (var t = 0; t < Tables.Count; t++)
        {
            var table = Tables[t];

            if (table.IsEmpty) continue;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);
            var s6 = table.GetStorage<C6>(Identity.None);
            var s7 = table.GetStorage<C7>(Identity.None);

            action(table.Count, s1, s2, s3, s4, s5, s6, s7);
        }
        
        Archetypes.Unlock();
    }
    
    public void RunParallel(Action<int, C1[], C2[], C3[], C4[], C5[], C6[], C7[]> action)
    {
        Archetypes.Lock();

        Parallel.For(0, Tables.Count, t =>
        {
            var table = Tables[t];
            
            if (table.IsEmpty) return;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);
            var s6 = table.GetStorage<C6>(Identity.None);
            var s7 = table.GetStorage<C7>(Identity.None);
            
            action(table.Count, s1, s2, s3, s4, s5, s6, s7);
        });
        
        Archetypes.Unlock();
    }
}

public class Query<C1, C2, C3, C4, C5, C6, C7, C8> : Query
    where C1 : struct
    where C2 : struct
    where C3 : struct
    where C4 : struct
    where C5 : struct
    where C6 : struct
    where C7 : struct
    where C8 : struct
{
    public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
    {
    }

    
    public RefValueTuple<C1, C2, C3, C4, C5, C6, C7, C8> Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var s1 = table.GetStorage<C1>(Identity.None);
        var s2 = table.GetStorage<C2>(Identity.None);
        var s3 = table.GetStorage<C3>(Identity.None);
        var s4 = table.GetStorage<C4>(Identity.None);
        var s5 = table.GetStorage<C5>(Identity.None);
        var s6 = table.GetStorage<C6>(Identity.None);
        var s7 = table.GetStorage<C7>(Identity.None);
        var s8 = table.GetStorage<C8>(Identity.None);
        return new RefValueTuple<C1, C2, C3, C4, C5, C6, C7, C8>(ref s1[meta.Row], ref s2[meta.Row],
            ref s3[meta.Row], ref s4[meta.Row], ref s5[meta.Row],
            ref s6[meta.Row], ref s7[meta.Row], ref s8[meta.Row]);
    }

    public void Run(Action<int, C1[], C2[], C3[], C4[], C5[], C6[], C7[], C8[]> action)
    {
        Archetypes.Lock();
        
        for (var t = 0; t < Tables.Count; t++)
        {
            var table = Tables[t];

            if (table.IsEmpty) continue;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);
            var s6 = table.GetStorage<C6>(Identity.None);
            var s7 = table.GetStorage<C7>(Identity.None);
            var s8 = table.GetStorage<C8>(Identity.None);

            action(table.Count, s1, s2, s3, s4, s5, s6, s7, s8);
        }
        
        Archetypes.Unlock();
    }
    
    public void RunParallel(Action<int, C1[], C2[], C3[], C4[], C5[], C6[], C7[], C8[]> action)
    {
        Archetypes.Lock();

        Parallel.For(0, Tables.Count, t =>
        {
            var table = Tables[t];
            
            if (table.IsEmpty) return;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);
            var s6 = table.GetStorage<C6>(Identity.None);
            var s7 = table.GetStorage<C7>(Identity.None);
            var s8 = table.GetStorage<C8>(Identity.None);
            
            action(table.Count, s1, s2, s3, s4, s5, s6, s7, s8);
        });
        
        Archetypes.Unlock();
    }
}

public class Query<C1, C2, C3, C4, C5, C6, C7, C8, C9> : Query
    where C1 : struct
    where C2 : struct
    where C3 : struct
    where C4 : struct
    where C5 : struct
    where C6 : struct
    where C7 : struct
    where C8 : struct
    where C9 : struct
{
    public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
    {
    }

    
    public RefValueTuple<C1, C2, C3, C4, C5, C6, C7, C8, C9> Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var s1 = table.GetStorage<C1>(Identity.None);
        var s2 = table.GetStorage<C2>(Identity.None);
        var s3 = table.GetStorage<C3>(Identity.None);
        var s4 = table.GetStorage<C4>(Identity.None);
        var s5 = table.GetStorage<C5>(Identity.None);
        var s6 = table.GetStorage<C6>(Identity.None);
        var s7 = table.GetStorage<C7>(Identity.None);
        var s8 = table.GetStorage<C8>(Identity.None);
        var s9 = table.GetStorage<C9>(Identity.None);
        return new RefValueTuple<C1, C2, C3, C4, C5, C6, C7, C8, C9>(ref s1[meta.Row], ref s2[meta.Row],
            ref s3[meta.Row], ref s4[meta.Row], ref s5[meta.Row],
            ref s6[meta.Row], ref s7[meta.Row], ref s8[meta.Row], ref s9[meta.Row]);
    }

    public void Run(Action<int, C1[], C2[], C3[], C4[], C5[], C6[], C7[], C8[], C9[]> action)
    {
        Archetypes.Lock();
        
        for (var t = 0; t < Tables.Count; t++)
        {
            var table = Tables[t];
            
            if (table.IsEmpty) continue;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);
            var s6 = table.GetStorage<C6>(Identity.None);
            var s7 = table.GetStorage<C7>(Identity.None);
            var s8 = table.GetStorage<C8>(Identity.None);
            var s9 = table.GetStorage<C9>(Identity.None);
            
            action(table.Count, s1, s2, s3, s4, s5, s6, s7, s8, s9);
        }
        
        Archetypes.Unlock();
    }
    
    public void RunParallel(Action<int, C1[], C2[], C3[], C4[], C5[], C6[], C7[], C8[], C9[]> action)
    {
        Archetypes.Lock();

        Parallel.For(0, Tables.Count, t =>
        {
            var table = Tables[t];
            
            if (table.IsEmpty) return;

            var s1 = table.GetStorage<C1>(Identity.None);
            var s2 = table.GetStorage<C2>(Identity.None);
            var s3 = table.GetStorage<C3>(Identity.None);
            var s4 = table.GetStorage<C4>(Identity.None);
            var s5 = table.GetStorage<C5>(Identity.None);
            var s6 = table.GetStorage<C6>(Identity.None);
            var s7 = table.GetStorage<C7>(Identity.None);
            var s8 = table.GetStorage<C8>(Identity.None);
            var s9 = table.GetStorage<C9>(Identity.None);
            
            action(table.Count, s1, s2, s3, s4, s5, s6, s7, s8, s9);
        });
        
        Archetypes.Unlock();
    }
}