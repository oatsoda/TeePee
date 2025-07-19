```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.4351/24H2/2024Update/HudsonValley)
Intel Core i7-8750H CPU 2.20GHz (Coffee Lake), 1 CPU, 12 logical and 6 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.3 (9.0.325.11113), X64 RyuJIT AVX2


```
| Method                       | Mean          | Error       | StdDev      | Gen0      | Gen1      | Gen2     | Allocated  |
|----------------------------- |--------------:|------------:|------------:|----------:|----------:|---------:|-----------:|
| FiveRulesAddAndBuild         |      4.647 μs |   0.0644 μs |   0.0571 μs |    2.2049 |    0.0458 |        - |   10.16 KB |
| FiveHundredRulesAddAndBuild  |    621.497 μs |   7.1377 μs |   6.3274 μs |  171.8750 |   90.8203 |        - |  918.49 KB |
| FiveThousandRulesAddAndBuild | 32,420.454 μs | 519.3052 μs | 761.1908 μs | 1687.5000 | 1125.0000 | 187.5000 | 9230.87 KB |
| FiveRulesBuild               |      1.751 μs |   0.0167 μs |   0.0131 μs |    0.8812 |    0.0076 |        - |    4.05 KB |
| FiveHundredRulesBuild        |    209.725 μs |   2.9317 μs |   2.4481 μs |   69.8242 |   23.1934 |        - |  325.02 KB |
| FiveThousandRulesBuild       |  3,206.731 μs |  60.2048 μs |  90.1116 μs |  554.6875 |  449.2188 |        - | 3242.99 KB |
