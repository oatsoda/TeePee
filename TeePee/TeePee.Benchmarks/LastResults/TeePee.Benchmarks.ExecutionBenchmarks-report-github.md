```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4351/24H2/2024Update/HudsonValley)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2


```
| Method                 | Mean         | Error      | StdDev     | Gen0      | Gen1    | Allocated  |
|----------------------- |-------------:|-----------:|-----------:|----------:|--------:|-----------:|
| FiveMismatches         |     9.857 μs |  0.0833 μs |  0.0739 μs |    3.3264 |  0.0153 |   15.31 KB |
| FiveHundredMismatches  |   934.433 μs | 11.8259 μs |  9.8751 μs |  328.1250 |       - | 1531.25 KB |
| FiveThousandMismatches | 9,243.704 μs | 86.8184 μs | 81.2100 μs | 3328.1250 | 15.6250 | 15312.5 KB |
