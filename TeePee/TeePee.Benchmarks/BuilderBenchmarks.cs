using BenchmarkDotNet.Attributes;
using System.Net;

namespace TeePee.Benchmarks;

[MemoryDiagnoser]
public class BuilderBenchmarks
{
    private static void AddRule(TeePeeBuilder builder, int number)
    {
        builder
            .ForRequest($"https://url{number}.com/test{number}", HttpMethod.Post)
            .ThatContainsQueryParam("a", "b")
            .ThatContainsHeader("h", "v")
            .ThatHasBody(new { test = "123" })
            .Responds()
            .WithStatus(HttpStatusCode.Created)
            .WithHeader("r", "v")
            .WithBody(new { Id = "abc" });
    }

    [Benchmark]
    public async Task FiveRulesAddAndBuild()
    {
        var builder = new TeePeeBuilder();
        for (int i = 0; i < 5; i++)
            AddRule(builder, i);
        await builder.Build();
    }

    [Benchmark]
    public async Task FiveHundredRulesAddAndBuild()
    {
        var builder = new TeePeeBuilder();
        for (int i = 0; i < 500; i++)
            AddRule(builder, i);
        await builder.Build();
    }

    [Benchmark]
    public async Task FiveThousandRulesAddAndBuild()
    {
        var builder = new TeePeeBuilder();
        for (int i = 0; i < 5000; i++)
            AddRule(builder, i);
        await builder.Build();
    }

    private readonly TeePeeBuilder m_FiveRulesBuilder = new();
    private readonly TeePeeBuilder m_FiveHundredRulesBuilder = new();
    private readonly TeePeeBuilder m_FiveThousandRulesBuilder = new();


    [GlobalSetup]
    public Task GlobalSetup()
    {
        for (int i = 0; i < 5; i++)
            AddRule(m_FiveRulesBuilder, i);

        for (int i = 0; i < 500; i++)
            AddRule(m_FiveHundredRulesBuilder, i);

        for (int i = 0; i < 5000; i++)
            AddRule(m_FiveThousandRulesBuilder, i);

        return Task.CompletedTask;
    }

    [Benchmark]
    public async Task FiveRulesBuild()
    {
        await m_FiveRulesBuilder.Build();
    }

    [Benchmark]
    public async Task FiveHundredRulesBuild()
    {
        await m_FiveHundredRulesBuilder.Build();
    }

    [Benchmark]
    public async Task FiveThousandRulesBuild()
    {
        await m_FiveThousandRulesBuilder.Build();
    }
}