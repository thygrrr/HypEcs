using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ECS;

namespace Benchmark;


[SimpleJob(RuntimeMoniker.Net80)]
[RPlotExporter]
public class SimpleEntityBenchmarks
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [Params(100_000_000)] 
    public int entityCount { get; set; }

    private static readonly Random random = new(1337);

    private World _world = null!;
    
    private Query<Vector3> _queryV3 = null!;
    private Vector3[] _vectors = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        _queryV3 = _world.Query<Vector3>().Build();
        _vectors = new Vector3[entityCount];
        

        for (var i = 0; i < entityCount; i+=4)
        {
            _world.Spawn().Add<Vector3>().Id();
            _world.Spawn().Add<Vector3>().Add<int>().Id();
            _world.Spawn().Add<Vector3>().Add<float>().Id();
            _world.Spawn().Add<Vector3>().Add<long>().Id();
        }
    }

    [Benchmark]
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

    [Benchmark]
    public void AddECSVector3Lambda()
    {
        _queryV3.Run((ref Vector3 v) => { v += Vector3.One; });
    }

    [Benchmark]
    public void AddECSVector3ParallelLambda()
    {
        _queryV3.RunParallel((ref Vector3 v) => { v += Vector3.One; });
    }

    [Benchmark]
    public void AddECSVector3Delegate()
    {
        _queryV3.Run(delegate (ref Vector3 v) { v += Vector3.One; });
    }

    [Benchmark]
    public void AddECSVector3ParallelDelegate()
    {
        _queryV3.RunParallel(delegate (ref Vector3 v) { v += Vector3.One; });
    }
}