using BenchmarkDotNet.Attributes;
using System.Net;
using System.Net.Http.Json;

namespace TeePee.Benchmarks;

[MemoryDiagnoser]
public class ExecutionBenchmarks
{
    private HttpClient? m_HttpClient;

    public static readonly string[] FiveUrls = new[]
    {
        "https://one.com/test1",
        "https://two.com/test2",
        "https://thr.com/test3",
        "https://fou.com/test4",
        "https://fiv.com/test5"
    };

    private static JsonContent m_RequestBody = JsonContent.Create(new { test = "123" });

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var builder = new TeePeeBuilder();
        foreach (var url in FiveUrls)
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

        m_HttpClient = (await builder.Build()).Manual().CreateClient();
    }

    [Benchmark]
    public void FiveMismatches()
    {
        foreach (var url in FiveUrls)
            m_HttpClient!.PostAsync(url, m_RequestBody);
    }

    [Benchmark]
    public void FiveHundredMismatches()
    {
        for (int i = 0; i < 100; i++)
            foreach (var url in FiveUrls)
                m_HttpClient!.PostAsync(url, m_RequestBody);
    }

    [Benchmark]
    public void FiveThousandMismatches()
    {
        for (int i = 0; i < 1000; i++)
            foreach (var url in FiveUrls)
                m_HttpClient!.PostAsync(url, m_RequestBody);
    }
}