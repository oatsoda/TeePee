```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4351/24H2/2024Update/HudsonValley)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2


```
| Method                 | Mean         | Error       | StdDev      | Median       | Gen0      | Allocated  |
|----------------------- |-------------:|------------:|------------:|-------------:|----------:|-----------:|
| FiveMismatches         |     6.685 μs |   0.1003 μs |   0.0837 μs |     6.678 μs |    2.2430 |   10.31 KB |
| FiveHundredMismatches  |   608.754 μs |  12.0592 μs |  12.9032 μs |   609.403 μs |  223.6328 | 1031.25 KB |
| FiveThousandMismatches | 6,295.976 μs | 124.7602 μs | 317.5544 μs | 6,189.913 μs | 2242.1875 | 10312.5 KB |
