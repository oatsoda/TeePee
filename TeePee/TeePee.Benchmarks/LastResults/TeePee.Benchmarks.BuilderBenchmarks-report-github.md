```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4351/24H2/2024Update/HudsonValley)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2


```
| Method                 | Mean          | Error       | StdDev        | Median        | Gen0      | Gen1      | Gen2     | Allocated  |
|----------------------- |--------------:|------------:|--------------:|--------------:|----------:|----------:|---------:|-----------:|
| FiveRulesBuild         |      4.527 μs |   0.0762 μs |     0.0676 μs |      4.498 μs |    2.2049 |    0.0458 |        - |   10.16 KB |
| FiveHundredRulesBuild  |    611.641 μs |   3.8588 μs |     3.4207 μs |    610.810 μs |  161.1328 |  121.0938 |        - |  918.49 KB |
| FiveThousandRulesBuild | 31,011.606 μs | 863.3439 μs | 2,532.0383 μs | 29,832.475 μs | 1500.0000 | 1000.0000 | 437.5000 | 9230.41 KB |
