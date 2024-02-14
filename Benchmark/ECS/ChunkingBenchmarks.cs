using System.Numerics;
using BenchmarkDotNet.Attributes;
using fennecs;

namespace Benchmark.ECS;

[SimpleJob]
[ThreadingDiagnoser]
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
public class ChunkingBenchmarks
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [Params(1_000_000)] public int entityCount { get; set; } = 1_000_000;
    [Params(4096, 16384)] public int chunkSize { get; set; } = 16384;

    private static readonly Random random = new(1337);

    private World _world = null!;

    private Query<Vector3> _queryV3 = null!;
    private Vector3[] _vectorsRaw = null!;

    [GlobalSetup]
    public void Setup()
    {
        ThreadPool.SetMaxThreads(48, 24);
        using var countdown = new CountdownEvent(1000);
        for (var i = 0; i < 1000; i++)
        {
            // ReSharper disable once AccessToDisposedClosure
            ThreadPool.QueueUserWorkItem(_ => { countdown.Signal();});
        }
        countdown.Wait();
        Thread.Yield();

        _world = new World();
        _queryV3 = _world.Query<Vector3>().Build();
        _vectorsRaw = new Vector3[entityCount];

        for (var i = 0; i < entityCount; i++)
        {
            _vectorsRaw[i] = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());

            //Multiple unused components added to create fennecs archetype fragmentation, which is used as basis for many parallel processing partitions.
            switch (i % 4)
            {
                case 0:
                    _world.Spawn().Add(_vectorsRaw[i]).Id();
                    break;
                case 1:
                    _world.Spawn().Add(_vectorsRaw[i]).Add<int>().Id();
                    break;
                case 2:
                    _world.Spawn().Add(_vectorsRaw[i]).Add<double>().Id();
                    break;
                case 3:
                    _world.Spawn().Add(_vectorsRaw[i]).Add<float>().Id();
                    break;
            }
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _queryV3.Dispose();
        _world.Dispose();
    }


    private static readonly Vector3 UniformConstantVector = new(3, 4, 5);

    //[Benchmark]
    public void CrossProduct_Run()
    {
        _queryV3.Run(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); });
    }

    [Benchmark]
    public void CrossProduct_RunParallel()
    {
        _queryV3.RunParallel(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); }, chunkSize);
    }

    [Benchmark]
    public void CrossProduct_Job()
    {
        _queryV3.Job(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); }, chunkSize);
    }
}