using System.Net;
using System.Net.Http.Json;
using BenchmarkDotNet.Attributes;

namespace TeePee.Benchmarks;

[MemoryDiagnoser]
public class ExecutionBenchmarks
{
    private HttpClient? m_HttpClient;

    [GlobalSetup]
    public void GlobalSetup()
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

        m_HttpClient = builder.Build().Manual().CreateClient();
    }

    [Benchmark]
    public void FiveMismatches()
    {
        foreach (var url in Data.Urls)
            m_HttpClient!.PostAsync(url, JsonContent.Create(new { test = "123" }));
    }
}