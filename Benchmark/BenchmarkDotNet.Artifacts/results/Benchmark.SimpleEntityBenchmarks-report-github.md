```

BenchmarkDotNet v0.13.12, Windows 11 (10.0.22631.3085/23H2/2023Update/SunValley3)
AMD Ryzen 9 5900X, 1 CPU, 24 logical and 12 physical cores
.NET SDK 8.0.101
  [Host]   : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
  ShortRun : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2

Job=ShortRun  Platform=X64  IterationCount=3  
LaunchCount=1  RunStrategy=Monitoring  WarmupCount=3  

```
| Method                               | entityCount | Mean       | Error      | StdDev    | Median     | Ratio | RatioSD |
|------------------------------------- |------------ |-----------:|-----------:|----------:|-----------:|------:|--------:|
| AddPlainVector3Array                 | 1000000     | 1,094.6 μs | 3,855.9 μs | 211.35 μs | 1,027.5 μs |  1.39 |    0.33 |
| AddPlainVector3ArrayEC               | 1000000     |   760.0 μs | 1,191.8 μs |  65.33 μs |   729.8 μs |  0.96 |    0.02 |
| AddPlainVector3Span                  | 1000000     |   649.2 μs |   223.5 μs |  12.25 μs |   647.4 μs |  0.82 |    0.04 |
| AddECSHypStyleArray                  | 1000000     |   792.6 μs |   980.8 μs |  53.76 μs |   768.0 μs |  1.00 |    0.00 |
| AddECSHypStyleSpan                   | 1000000     |   885.5 μs | 2,119.2 μs | 116.16 μs |   862.9 μs |  1.11 |    0.07 |
| AddECSVector3Delegate                | 1000000     | 1,251.5 μs | 2,268.4 μs | 124.34 μs | 1,321.9 μs |  1.58 |    0.16 |
| AddECSVector3ParallelDelegate        | 1000000     |   597.4 μs | 2,286.5 μs | 125.33 μs |   590.7 μs |  0.75 |    0.11 |
| AddECSVector3ParallelDelegateChunked | 1000000     | 1,261.3 μs |   977.0 μs |  53.55 μs | 1,291.5 μs |  1.59 |    0.10 |
