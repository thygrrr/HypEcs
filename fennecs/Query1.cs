// SPDX-License-Identifier: MIT

using Schedulers;

namespace fennecs;

public class Query<C> : Query
{
    private readonly List<JobHandle> _handles;
    private readonly List<Workload<C>> _workloads;
    private readonly CountdownEvent _countdown;


    public Query(Archetypes archetypes, Mask mask, List<Table> tables) : base(archetypes, mask, tables)
    {
        _countdown = new CountdownEvent(1);
        _handles = new List<JobHandle>(8192);
        _workloads = new List<Workload<C>>(8192);
        for (var i = 0; i < 8192; i++) _workloads.Add(new Workload<C>());
    }

    private sealed class Workload<C1> : IJob
    {
        public CountdownEvent Countdown { get; set; }
        public Memory<C1> Memory { get; set; }
        public RefAction_C<C1> Action { get; set; }

        public void Execute()
        {
            foreach (ref var c in Memory.Span) Action(ref c);
        }

        public readonly WaitCallback WaitCallback;

        public Workload()
        {
            WaitCallback = CallBack;
        }

        private void CallBack(object? state)
        {
            Execute();
            Countdown.Signal();
        }
    }
    
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

    private void Work(Span<C> storage, RefAction_C<C> action, ref CountdownEvent countdown)
    {
        foreach (ref var c in storage) action(ref c);
        countdown.Signal();
    }

    public void Job(RefAction_C<C> action, int chunkSize = int.MaxValue)
    {
        Archetypes.Lock();
        _handles.Clear();
        var load = 0;
        
        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None);
            var length = table.Count;

            var partitions = Math.Max(length / chunkSize, 1);
            var partitionSize = length / partitions;

            for (var partition = 0; partition < partitions; partition++)
            {
                var workload = _workloads[load++];
                workload.Action = action;
                workload.Memory = storage.AsMemory(partition * partitionSize, partitionSize);
                _handles.Add(Scheduler.Schedule(workload));
            }
        }

        Scheduler.Flush();
        JobHandle.CompleteAll(_handles);
        Archetypes.Unlock();
    }


    public void RunParallel(RefAction_C<C> action, int chunkSize = int.MaxValue)
    {
        Archetypes.Lock();
        _countdown.Reset();
        var load = 0;
        
        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None);
            var items = table.Count;

            var partitions = items / chunkSize + items % chunkSize != 0 ? 1 : 0;
            var partitionSize = items / partitions;

            for (var partition = 0; partition < partitions; partition++)
            {
                _countdown.AddCount();
                
                var workload = _workloads[load++];
                workload.Action = action;
                var start = partition * partitionSize;
                var length = Math.Min(partitionSize, storage.Length - start);
                workload.Memory = storage.AsMemory(start, length);
                workload.Countdown = _countdown;
                ThreadPool.QueueUserWorkItem(workload.WaitCallback);
            }

            /*
            for (var partition = 0; partition < partitions; partition++)
            {
                _countdown.AddCount();
                var memory = storage.AsMemory(partition * partitionSize, partitionSize);
                ThreadPool.QueueUserWorkItem(delegate { Work(memory.Span, action, ref _countdown); });
            }
            */
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
            var storage = table.GetStorage<C>(Identity.None);

            var length = table.Count;

            var partitions = Math.Max(length / chunkSize, 1);
            var partitionSize = length / partitions;

            for (var partition = 0; partition < partitions; partition++)
            {
                _countdown.AddCount();

                ThreadPool.QueueUserWorkItem(delegate(int part)
                {
                    foreach (ref var c in storage.AsSpan(part * partitionSize, partitionSize))
                    {
                        action(ref c, uniform);
                    }

                    // ReSharper disable once AccessToDisposedClosure
                    _countdown.Signal();
                }, partition, preferLocal: true);
            }

            /*
            //Optimization: Also process one partition right here on the calling thread.
            foreach (ref var c in storage.AsSpan(0, partitionSize))
            {
                action(ref c, uniform);
            }
            */
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