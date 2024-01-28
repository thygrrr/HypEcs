using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace Benchmark;

[MemoryDiagnoser(true)]
public class V3Benchmarks
{
    [Params(1000, 1000000)] 
    public int entityCount { get; set; }

    private static readonly Random random = new(1337);

    private Vector3[] _input = null!;
    private float[] _output = null!;
    
    [GlobalSetup]
    public void Setup()
    {
        _input = Enumerable.Range(0, entityCount).Select(_ => new Vector3(random.Next(), random.Next(), random.Next())).ToArray();
        _output = new float[entityCount];
    }

    [Benchmark]
    public void PerItemDot()
    {
        for (var i = 0; i < entityCount; i++)
        {
            _output[i] = Vector3.Dot(_input[i], new Vector3(1, 2, 3));
        }
    }

    [Benchmark]
    public void PerItemDotParallel()
    {
        Parallel.For(0, entityCount, i =>
        {
            _output[i] = Vector3.Dot(_input[i], new Vector3(1, 2, 3));
        });
    }

    public void PerItemDotSpan()
    {
        var va = new Vector3(1, 2, 3);
        var input = _input.AsSpan();
        var output = _output.AsSpan();
        for (var i = 0; i < entityCount; i++)
        {
            output[i] = Vector3.Dot(input[i], va);
        }
    }
}