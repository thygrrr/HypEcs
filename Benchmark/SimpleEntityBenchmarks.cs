using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ECS;

namespace Benchmark;


[ShortRunJob(RuntimeMoniker.Net80)]
//[ThreadingDiagnoser]
//[MemoryDiagnoser]
public class SimpleEntityBenchmarks
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [Params(10_000_000)] 
    public int entityCount { get; set; }

    //private static readonly Random random = new(1337);

    private World _world = null!;
    
    private Query<Vector3> _queryV3 = null!;
    private Vector3[] _vectors = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _queryV3 = _world.Query<Vector3>().Build();
        _vectors = new Vector3[entityCount];
        

        for (var i = 0; i < entityCount; i+=8)
        {
            _world.Spawn().Add<Vector3>().Id();
            _world.Spawn().Add<Vector3>().Add<int>().Id();
            _world.Spawn().Add<Vector3>().Add<byte>().Id();
            _world.Spawn().Add<Vector3>().Add<double>().Id();
            _world.Spawn().Add<Vector3>().Add<short>().Id();
            _world.Spawn().Add<Vector3>().Add<ushort>().Id();
            _world.Spawn().Add<Vector3>().Add<float>().Id();
            _world.Spawn().Add<Vector3>().Add<long>().Add<float>().Id();
        }
    }


    //[Benchmark]
    public void AddPlainVector3Array()
    {
        for (var i = 0; i < entityCount; i++)
        {
            _vectors[i] += Vector3.One;
        }
    }

    [Benchmark]
    public void AddPlainVector3Span()
    {
        foreach (ref var v in _vectors.AsSpan())
        {
            v += Vector3.One;
        }
    }

    //[Benchmark]
    public void AddECSVector3Lambda()
    {
        _queryV3.Run((ref Vector3 v) => { v += Vector3.One; });
    }

    //[Benchmark]
    public void AddECSVector3ParallelLambda()
    {
        _queryV3.RunParallel((ref Vector3 v) => { v += Vector3.One; });
    }

    public void AddECSHypStyleArray()
    {
        _queryV3.RunHypStyle(delegate(int count, Vector3[] vectors)
        {
            for (var i = 0; i < count; i++)
            {
                vectors[i] += Vector3.One;
            }
        });
    }

    [Benchmark(Baseline = true)]
    public void AddECSHypStyleParallel()
    {
        var opts = new ParallelOptions {MaxDegreeOfParallelism = 16};
        _queryV3.RunHypStyle(delegate(int count, Vector3[] vectors)
        {
            Parallel.For(0, count, opts, delegate (int i) { vectors[i] += Vector3.One; });
        });
    }

    [Benchmark]
    public void AddECSVector3Delegate()
    {
        _queryV3.Run(delegate (ref Vector3 v) { v += Vector3.One; });
    }

    [Benchmark]
    public void AddECSVector3ParallelDelegate()
    {
        _queryV3.RunParallel(delegate(ref Vector3 v) { v += Vector3.One; });
    }

    [Benchmark]
    public async Task AddECSVector3Channeled()
    {
        await _queryV3.RunParallelChanneled(delegate(ref Vector3 v) { v += Vector3.One; });
    }

    [Benchmark]
    public async Task AddECSVector3TaskedDelegate()
    {
        await _queryV3.RunTasked(delegate(ref Vector3 v) { v += Vector3.One; });
    }

    [Benchmark]
    public void AddECSVector3ParallelDelegateChunked()
    {
        _queryV3.RunParallelChunked(delegate(ref Vector3 v) { v += Vector3.One; });
    }
}