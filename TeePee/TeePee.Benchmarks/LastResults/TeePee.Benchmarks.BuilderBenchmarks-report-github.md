```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4351/24H2/2024Update/HudsonValley)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2


```
| Method                 | Mean          | Error       | StdDev      | Gen0      | Gen1     | Gen2     | Allocated  |
|----------------------- |--------------:|------------:|------------:|----------:|---------:|---------:|-----------:|
| FiveRulesBuild         |      4.585 μs |   0.0901 μs |   0.0964 μs |    2.2049 |   0.0458 |        - |   10.16 KB |
| FiveHundredRulesBuild  |    624.783 μs |  11.7136 μs |  10.9569 μs |  161.1328 | 121.0938 |        - |  918.48 KB |
| FiveThousandRulesBuild | 29,148.894 μs | 572.0048 μs | 763.6100 μs | 1466.6667 | 933.3333 | 400.0000 | 9230.37 KB |
