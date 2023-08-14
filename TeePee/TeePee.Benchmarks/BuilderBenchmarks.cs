using BenchmarkDotNet.Attributes;
using System.Net;

namespace TeePee.Benchmarks;

[MemoryDiagnoser]
public class BuilderBenchmarks
{
    [Benchmark]
    public void FiveRulesBuild()
    {
        var builder = new TeePeeBuilder();
        foreach (var url in Data.Urls)
        {
            builder.ForRequest(url, HttpMethod.Post)
                   .ThatContainsQueryParam("a", "b")
                   .ThatContainsHeader("h", "v")
                   .ThatHasBody(new { test = "123" })
                   .Responds()
                   .WithStatus(HttpStatusCode.Created)
                   .WithHeader("r", "v")
                   .WithBody(new { Id = "abc" });
        }

        builder.Build();
    }
}