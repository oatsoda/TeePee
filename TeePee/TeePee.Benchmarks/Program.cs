using BenchmarkDotNet.Running;
using TeePee.Benchmarks;

BenchmarkRunner.Run<BuilderBenchmarks>();
BenchmarkRunner.Run<ExecutionBenchmarks>();