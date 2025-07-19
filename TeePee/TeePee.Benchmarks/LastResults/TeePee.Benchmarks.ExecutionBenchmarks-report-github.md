```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4351/24H2/2024Update/HudsonValley)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2


```
| Method                 | Mean         | Error       | StdDev      | Gen0      | Allocated  |
|----------------------- |-------------:|------------:|------------:|----------:|-----------:|
| FiveMismatches         |     6.520 μs |   0.0996 μs |   0.0832 μs |    2.2430 |   10.31 KB |
| FiveHundredMismatches  |   605.344 μs |  11.5414 μs |  10.2311 μs |  223.6328 | 1031.25 KB |
| FiveThousandMismatches | 6,155.409 μs | 117.1098 μs | 164.1715 μs | 2242.1875 | 10312.5 KB |
