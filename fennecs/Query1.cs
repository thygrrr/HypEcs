// SPDX-License-Identifier: MIT

namespace fennecs;

public class Query<C>(Archetypes archetypes, Mask mask, List<Table> tables) : Query(archetypes, mask, tables)
{
    private readonly List<Work<C>> _jobs = new(512);
    
    private readonly CountdownEvent _countdown = new(1);
    
    private class Work<C1> : IThreadPoolWorkItem
    {
        public Memory<C1> Memory = null!;
        public RefAction_C<C1> Action = null!;
        public CountdownEvent CountDown = null!;
        public WaitCallback WaitCallback => Execute;

        private void Execute(object? state) => Execute();

        public void Execute()
        {
            using var _ = Memory.Pin();
            foreach (ref var c in Memory.Span) Action(ref c);
            CountDown.Signal();
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

    public void RunParallel(RefAction_C<C> action, int chunkSize = int.MaxValue)
    {
        Archetypes.Lock();
        _countdown.Reset();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None);

            var count = table.Count; // storage.Length is the capacity, not the count.
            var partitions = count / chunkSize + Math.Sign(count % chunkSize);

            for (var chunk = 0; chunk < partitions; chunk++)
            {
                _countdown.AddCount();

                var start = chunk * chunkSize;
                var length = Math.Min(chunkSize, count - start);

                var job = JobPool<Work<C>>.Rent();
                job.Memory = storage.AsMemory(start, length);
                job.Action = action;
                job.CountDown = _countdown;
                _jobs.Add(job);
                ThreadPool.QueueUserWorkItem(job.WaitCallback);
            }
        }

        _countdown.Signal();
        _countdown.Wait();
        JobPool<Work<C>>.Return(_jobs);
        Archetypes.Unlock();
    }

    public void Job(RefAction_C<C> action, int chunkSize = int.MaxValue)
    {
        Archetypes.Lock();
        _countdown.Reset();

        foreach (var table in Tables)
        {
            if (table.IsEmpty) continue;
            var storage = table.GetStorage<C>(Identity.None);

            var count = table.Count; // storage.Length is the capacity, not the count.
            var partitions = count / chunkSize + Math.Sign(count % chunkSize);

            for (var chunk = 0; chunk < partitions; chunk++)
            {
                _countdown.AddCount();

                var start = chunk * chunkSize;
                var length = Math.Min(chunkSize, count - start);

                var job = JobPool<Work<C>>.Rent();
                job.Memory = storage.AsMemory(start, length);
                job.Action = action;
                job.CountDown = _countdown;
                _jobs.Add(job);
                ThreadPool.UnsafeQueueUserWorkItem(job, true);
            }
        }

        _countdown.Signal();
        _countdown.Wait();
        JobPool<Work<C>>.Return(_jobs);
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