namespace fennecs;


internal class Work<C1> : IThreadPoolWorkItem
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


internal class Work<C1, U> : IThreadPoolWorkItem
{
    public Memory<C1> Memory = null!;
    public RefAction_CU<C1, U> Action = null!;
    public CountdownEvent CountDown = null!;
    public U Uniform;
    
    public WaitCallback WaitCallback => Execute;

    private void Execute(object? state) => Execute();

    public void Execute()
    {
        using var _ = Memory.Pin();
        foreach (ref var c in Memory.Span) Action(ref c, Uniform);
        CountDown.Signal();
    }
}
