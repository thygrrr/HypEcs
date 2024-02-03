using System.Numerics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using ECS;

namespace Benchmark;


[ShortRunJob(RuntimeMoniker.Net80)]
[ThreadingDiagnoser]
[MemoryDiagnoser]
public class SimpleEntityBenchmarks
{
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    [Params(10_000_000)] 
    public int entityCount { get; set; }

    private static readonly Random random = new(1337);

    private World _world = null!;
    
    private Query<Vector3> _queryV3 = null!;
    private Vector3[] _vectorsRaw = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        ThreadPool.SetMaxThreads(4, 4);

        _world = new World();
        _queryV3 = _world.Query<Vector3>().Build();
        _vectorsRaw = new Vector3[entityCount];

        for (var i = 0; i < entityCount; i++)
        {
            _vectorsRaw[i] = new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle());
            
            //Multiple unused components added to create ECS archetype fragmentation, which is used as basis for many parallel processing partitions.
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

    private static readonly Vector3 UniformConstantVector = new(3, 4, 5);


    [Benchmark]
    //[Benchmark(Description = "for() loop over Array of Vector3 locally (raw data, baseline processing speed).")]
    public void Single_Direct_Array()
    {
        for (var i = 0; i < entityCount; i++)
        {
            _vectorsRaw[i] = Vector3.Cross(_vectorsRaw[i], UniformConstantVector);
        }
    }

    [Benchmark]
    //[Benchmark(Description = "foreach() loop over Span of Vector3 locally (raw data, baseline processing speed).")]
    public void Single_Direct_Span()
    {
        foreach (ref var v in _vectorsRaw.AsSpan())
        {
            v = Vector3.Cross(v, UniformConstantVector);
        }
    }

    [Benchmark]
    //[Benchmark(Description = "Parallel.For over Array of Vector3 locally (raw data, parallel speed).")]
    public void Parallel_Direct_Array()
    {
        Parallel.For(0, _vectorsRaw.Length,
            i =>
            {
                _vectorsRaw[i] = Vector3.Cross(_vectorsRaw[i], UniformConstantVector);
            });
    }

    [Benchmark]
    //[Benchmark(Description = "A lambda is passed each Vector3 by ref.")]
    public void Single_ECS_Lambda()
    {
        _queryV3.Run((ref Vector3 v) => { v = Vector3.Cross(v, UniformConstantVector); });
    }

    [Benchmark]
    //[Benchmark(Description = "Parallel.Foreach passes each Vector3 by ref to a lambda.")]
    public void Parallel_ECS_Lambda()
    {
        _queryV3.RunParallel((ref Vector3 v) => { v = Vector3.Cross(v, UniformConstantVector); });
    }

    [Benchmark(Baseline = true)]
    //[Benchmark(Baseline = true, Description = "Work on Array passed in by ECS in a delegate.")]
    public void Single_HypStyle_Array_Delegate()
    {
        _queryV3.RunHypStyle(delegate(int count, Vector3[] vectors)
        {
            for (var i = 0; i < count; i++)
            {
                vectors[i] = Vector3.Cross(vectors[i], UniformConstantVector);
            }
        });
    }

    [Benchmark]
    //[Benchmark(Description = "Array passed in by ECS in a delegate, processed locally in Parallel.For.")]
    public void Parallel_HypStyle_Array_Delegate()
    {
        var opts = new ParallelOptions {MaxDegreeOfParallelism = 16};
        _queryV3.RunHypStyle(delegate(int count, Vector3[] vectors)
        {
            Parallel.For(0, count, opts, delegate (int i) { vectors[i] = Vector3.Cross(vectors[i], UniformConstantVector); });
        });
    }

    [Benchmark]
    //[Benchmark(Description = "Work passed into delegate as ref Vector3.")]
    public void Single_ECS_Delegate()
    {
        _queryV3.Run(delegate (ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); });
    }

    [Benchmark]
    //[Benchmark(Description = "Work parallelized by Archetype, passed into delegate as ref Vector3.")]
    public void Parallel_ECS_Delegate_Archetype()
    {
        _queryV3.RunParallel(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); });
    }

    [Benchmark]
    //[Benchmark(Description = "Work split into chunks, passed to Workers which invoke delegates passing individual ref Vector3s.")]
    public async Task Parallel_ECS_Channeled()
    {
        await _queryV3.RunParallelChanneled(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); });
    }

    [Benchmark]
    //[Benchmark(Description = "Work split into Tasks per Archetype, each worker passing individual ref Vector3s to delegate.")]
    public async Task Parallel_ECS_Tasked()
    {
        await _queryV3.RunTasked(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); });
    }

    [Benchmark]
    //[Benchmark(Description = "Work split into chunks, each worker passing individual ref Vector3s to delegate.")]
    public void Parallel_ECS_Chunked()
    {
        _queryV3.RunParallelChunked(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); });
    }
}