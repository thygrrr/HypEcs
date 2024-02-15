// SPDX-License-Identifier: MIT

namespace fennecs;

public class Query<C>(Archetypes archetypes, Mask mask, List<Table> tables) : Query(archetypes, mask, tables)
{
    //private readonly List<JobHandle> _handles;
    //private readonly List<Workload<C>> _workloads = new(8192);
    private readonly CountdownEvent _countdown = new(1);


    //for (var i = 0; i < 8192; i++) _workloads.Add(new Workload<C>(_countdown));

    public ref C Get(Entity entity)
    {
        var meta = Archetypes.GetEntityMeta(entity.Identity);
        var table = Archetypes.GetTable(meta.TableId);
        var storage = table.GetStorage<C>(Identity.None);
        return ref storage[meta.Row];
    }

    #region Runners

    public void Run(RefAction_C<C> action)
    {
        Archetypes.Lock();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None).AsSpan(0, table.Count);
            foreach (ref var c in storage) action(ref c);
        }

        Archetypes.Unlock();
    }

    public void RunParallel(RefAction_C<C> action, int chunkSize = int.MaxValue)
    {
        Archetypes.Lock();
        _countdown.Reset();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var memory = table.Memory<C>(Identity.None);
            var partitions = memory.Length / chunkSize + Math.Sign(memory.Length % chunkSize);

            for (var chunk = 0; chunk < partitions; chunk++)
            {
                _countdown.AddCount();

                var start = chunk * chunkSize;
                var length = Math.Min(chunkSize, memory.Length - start);

                var workload = new Workload<C>(memory.Slice(start, length), action, _countdown);
                ThreadPool.QueueUserWorkItem(workload.WaitCallback);
            }
        }

        _countdown.Signal();
        _countdown.Wait();
        Archetypes.Unlock();
    }

    public void Run<U>(RefAction_CU<C, U> action, U uniform)
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


    public void RunParallel<U>(RefAction_CU<C, U> action, U uniform, int chunkSize = int.MaxValue)
    {
        Archetypes.Lock();
        _countdown.Reset();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var memory = table.Memory<C>(Identity.None);
            var partitions = memory.Length / chunkSize + Math.Sign(memory.Length % chunkSize);

            for (var chunk = 0; chunk < partitions; chunk++)
            {
                _countdown.AddCount();

                var start = chunk * chunkSize;
                var length = Math.Min(chunkSize, memory.Length - start);

                var workload = new WorkloadU<C,U>
                {
                    Action = action,
                    Uniform = uniform,
                    CountDown = _countdown,
                    Memory = memory.Slice(start, length),
                };
                
                ThreadPool.QueueUserWorkItem(workload.WaitCallback);
            }
        }

        _countdown.Signal();
        _countdown.Wait();
        Archetypes.Unlock();
    }


    public void RunParallel2Old<U>(RefAction_CU<C, U> action, U uniform, int chunkSize = int.MaxValue)
    {
        Archetypes.Lock();
        _countdown.Reset();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None);

            var items = table.Count;

            var partitions = items / chunkSize + items % chunkSize == 0 ? 0 : 1;
            var partitionSize = items / partitions;

            for (var partition = 0; partition < partitions; partition++)
            {
                _countdown.AddCount();

                ThreadPool.QueueUserWorkItem(delegate(int part)
                {
                    var start = part * partitionSize;
                    var length = Math.Min(partitionSize, items - start);
                    foreach (ref var c in storage.AsSpan(part * partitionSize, length))
                    {
                        action(ref c, uniform);
                    }

                    // ReSharper disable once AccessToDisposedClosure
                    _countdown.Signal();
                }, partition, preferLocal: true);
            }
        }

        _countdown.Signal();
        _countdown.Wait();
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

    public void RawParallel(Action<Memory<C>> action)
    {
        Archetypes.Lock();
        Parallel.ForEach(Tables, Options, table =>
        {
            if (table.IsEmpty) return; //TODO: This wastes a scheduled thread.
            action(table.GetStorage<C>(Identity.None).AsMemory(0, table.Count));
        });

        Archetypes.Unlock();
    }

    #endregion
}