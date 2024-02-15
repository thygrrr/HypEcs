using Benchmark.ECS;
using BenchmarkDotNet.Running;


BenchmarkSwitcher.FromAssembly(typeof(Benchmark.Base).Assembly).Run(args);

/*
var summary = new ChunkingBenchmarks();
summary.Setup();
summary.CrossProduct_RunParallel();
summary.Cleanup();
*/