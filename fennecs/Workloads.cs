namespace fennecs;

    internal struct Workload<C1>(Memory<C1> memory, RefAction_C<C1> action, CountdownEvent countDown)
    {
        private readonly void Execute(object? _)
        {
            foreach (ref var c in memory.Span) action(ref c);
            countDown.Signal();
        }
        public readonly WaitCallback WaitCallback => Execute;
    }

    internal struct WorkloadU<C1, U>
    {
        public required Memory<C1> Memory;
        public required RefAction_CU<C1, U> Action;
        public required CountdownEvent CountDown;
        public required U Uniform;

        private readonly void Execute(object? _)
        {
            foreach (ref var c in Memory.Span) Action(ref c, Uniform);
            CountDown.Signal();
        }

        public readonly WaitCallback WaitCallback => Execute;
    }

