﻿using System.Numerics;
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
    [Params(10_000, 1_000_000)] 
    public int entityCount { get; set; }

    private static readonly Random random = new(1337);

    private World _world = null!;
    
    private Query<Vector3> _queryV3 = null!;
    private Vector3[] _vectorsRaw = null!;

    [GlobalSetup]
    public void Setup()
    {
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
    private static readonly ParallelOptions options = new() {MaxDegreeOfParallelism = 12};

    //[Benchmark]
    //A lambda is passed each Vector3 by ref.
    public void Single_ECS_Lambda()
    {
        _queryV3.Run((ref Vector3 v) => { v = Vector3.Cross(v, UniformConstantVector); });
    }

    //[Benchmark]
    //Parallel.Foreach passes each Vector3 by ref to a lambda.
    public void Parallel_ECS_Lambda()
    {
        _queryV3.RunParallel((ref Vector3 v) => { v = Vector3.Cross(v, UniformConstantVector); });
    }

    [Benchmark(Baseline = true)]
    public void Single_Span_Delegate()
    {
        _queryV3.Run(delegate(Span<Vector3> vectors)
        {
            foreach(ref var v in vectors) v = Vector3.Cross(v, UniformConstantVector);
        });
    }

    //[Benchmark]
    //Work passed into delegate as ref Vector3.
    public void Single_ECS_Delegate()
    {
        _queryV3.Run(delegate (ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); });
    }

    //[Benchmark]
    //Work parallelized by Archetype, passed into delegate as ref Vector3.
    public void Single_ECS_Raw()
    {
        _queryV3.Raw(delegate(Memory<Vector3> vectors)
        {
            foreach (ref var v in vectors.Span)
            {
                v = Vector3.Cross(v, UniformConstantVector);
            }
        });
    }

    //[Benchmark]
    //Work parallelized by Archetype, passed into delegate as ref Vector3.
    public void Parallel_ECS_Delegate_Archetype()
    {
        _queryV3.RunParallel(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); });
    }

    //[Benchmark]
    //Work parallelized by Archetype, passed into delegate as ref Vector3.
    public void Parallel_ECS_Delegate_Chunk1k()
    {
        _queryV3.RunParallel(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); }, chunkSize: 1_000);
    }

    //[Benchmark]
    //Work parallelized by Archetype, passed into delegate as ref Vector3.
    public void Parallel_ECS_Delegate_Chunk100k()
    {
        _queryV3.RunParallel(delegate(ref Vector3 v) { v = Vector3.Cross(v, UniformConstantVector); }, chunkSize: 100_000);
    }

    [Benchmark]
    public void Parallel_ECS_Raw()
    {
        _queryV3.Raw(vectors =>
        {
            var slices = options.MaxDegreeOfParallelism;
            var queued = 0;
            
            for (var i = 1; i < slices; i++)
            {
                Interlocked.Increment(ref queued);
                
                ThreadPool.QueueUserWorkItem(slice =>
                {
                    foreach (ref var v in slice.Span)
                    {
                        v = Vector3.Cross(v, UniformConstantVector);
                    }
                    // ReSharper disable once AccessToModifiedClosure
                    Interlocked.Decrement(ref queued);
                }, vectors.Slice(i * vectors.Length / slices, vectors.Length / slices), preferLocal: true);
            }

            
            foreach (ref var v in vectors.Slice(0, entityCount / slices).Span)
            {
                v = Vector3.Cross(v, UniformConstantVector);
            }
            
            while (queued > 0) Thread.SpinWait(420);
        });
    }
    
}