using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace TeePee.Tests;

/// <summary>
/// Tests to ensure that re-use and Reset scenarios behave correctly.
/// </summary>
public class StateAndResetTests
{
    private readonly HttpRequestMessage m_MatchingHttpRequest;
    private readonly HttpRequestMessage m_MatchingHttpRequestTwo;
    private readonly Tracker m_MatchingTracker;

    private readonly TeePeeBuilder m_Builder;

    // ** For Injection Tests
    private readonly ServiceCollection m_Services;

    public StateAndResetTests()
    {
        m_MatchingHttpRequest = new(HttpMethod.Get, "http://unit.test/call");
        m_MatchingHttpRequestTwo = new(m_MatchingHttpRequest.Method, m_MatchingHttpRequest.RequestUri);

        m_Builder = new TeePeeBuilder();
        m_MatchingTracker = m_Builder
            .ForRequest(m_MatchingHttpRequest.RequestUri!.ToString(), m_MatchingHttpRequest.Method)
            .TrackRequest();


        m_Services = new ServiceCollection();
        m_Services.AddHttpClient();
        m_Services.AddHttpClient<AbsolutePathTypedHttpClient>();
    }

    #region Manual

    [Fact]
    public async Task RetainsState_AcrossMultipleManualUsesOfBuilder()
    {
        // Given
        await m_Builder.Manual().CreateClient().SendAsync(m_MatchingHttpRequest, TestCt);

        // When
        await m_Builder.Manual().CreateClient().SendAsync(m_MatchingHttpRequestTwo, TestCt);

        // Then
        m_MatchingTracker.WasCalled(2);
    }

    [Fact]
    public async Task Reset_ClearsState_AcrossMultipleManualUsesOfBuilder()
    {
        // Given
        await m_Builder.Manual().CreateClient().SendAsync(m_MatchingHttpRequest, TestCt);
        m_Builder.Reset();

        // When
        await m_Builder.Manual().CreateClient().SendAsync(m_MatchingHttpRequestTwo, TestCt);

        // Then
        m_MatchingTracker.WasCalled(1);
    }

    [Fact]
    public async Task Reset_ClearsState_ForSameManualUseOfBuilder()
    {
        // Given
        var client = m_Builder.Manual().CreateClient();
        await client.SendAsync(m_MatchingHttpRequest, TestCt);
        m_Builder.Reset();

        // When
        await client.SendAsync(m_MatchingHttpRequestTwo, TestCt);

        // Then
        m_MatchingTracker.WasCalled(1);
    }

    #endregion

    #region Default/Named Client

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RetainsState_AcrossMultipleDefaultOrNamedClientUsesOfBuilder(bool isDefaultClient)
    {
        // Given
        Func<IHttpClientFactory, HttpClient> createClient;
        if (isDefaultClient)
        {
            m_Services.AttachToDefaultClient(m_Builder);
            createClient = f => f.CreateClient();
        }
        else
        {
            m_Services.AttachToNamedClient(m_Builder, "a client");
            createClient = f => f.CreateClient("a client");
        }

        var httpClientFactory = m_Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        await createClient(httpClientFactory).SendAsync(m_MatchingHttpRequest, TestCt);

        // When
        await createClient(httpClientFactory).SendAsync(m_MatchingHttpRequestTwo, TestCt);

        // Then
        m_MatchingTracker.WasCalled(2);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reset_ClearsState_AcrossMultipleDefaultOrNamedClientUsesOfBuilder(bool isDefaultClient)
    {
        // Given
        Func<IHttpClientFactory, HttpClient> createClient;
        if (isDefaultClient)
        {
            m_Services.AttachToDefaultClient(m_Builder);
            createClient = f => f.CreateClient();
        }
        else
        {
            m_Services.AttachToNamedClient(m_Builder, "a client");
            createClient = f => f.CreateClient("a client");
        }

        var httpClientFactory = m_Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        await createClient(httpClientFactory).SendAsync(m_MatchingHttpRequest, TestCt);

        m_Builder.Reset();

        // When
        await createClient(httpClientFactory).SendAsync(m_MatchingHttpRequestTwo, TestCt);

        // Then
        m_MatchingTracker.WasCalled(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Reset_ClearsState_ForSameDefaultOrNamedClientUseOfBuilder(bool isDefaultClient)
    {
        // Given
        Func<IHttpClientFactory, HttpClient> createClient;
        if (isDefaultClient)
        {
            m_Services.AttachToDefaultClient(m_Builder);
            createClient = f => f.CreateClient();
        }
        else
        {
            m_Services.AttachToNamedClient(m_Builder, "a client");
            createClient = f => f.CreateClient("a client");
        }

        var httpClientFactory = m_Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        var client = createClient(httpClientFactory);

        await client.SendAsync(m_MatchingHttpRequest, TestCt);
        m_Builder.Reset();

        // When
        await client.SendAsync(m_MatchingHttpRequestTwo, TestCt);

        // Then
        m_MatchingTracker.WasCalled(1);
    }

    #endregion

    #region Typed Client

    [Fact]
    public async Task RetainsState_AcrossMultipleTypedClientUsesOfBuilder()
    {
        // Given
        m_Services.AttachToTypedClient<AbsolutePathTypedHttpClient>(m_Builder);
        var serviceProvider = m_Services.BuildServiceProvider();
        await serviceProvider.GetRequiredService<AbsolutePathTypedHttpClient>().Get();

        // When
        await serviceProvider.GetRequiredService<AbsolutePathTypedHttpClient>().Get();

        // Then
        m_MatchingTracker.WasCalled(2);
    }

    [Fact]
    public async Task Reset_ClearsState_AcrossMultipleTypedClientUsesOfBuilder()
    {
        // Given
        m_Services.AttachToTypedClient<AbsolutePathTypedHttpClient>(m_Builder);
        var serviceProvider = m_Services.BuildServiceProvider();
        await serviceProvider.GetRequiredService<AbsolutePathTypedHttpClient>().Get();
        m_Builder.Reset();

        // When
        await serviceProvider.GetRequiredService<AbsolutePathTypedHttpClient>().Get();

        // Then
        m_MatchingTracker.WasCalled(1);
    }

    [Fact]
    public async Task Reset_ClearsState_ForSameTypedClientUseOfBuilder()
    {
        // Given
        m_Services.AttachToTypedClient<AbsolutePathTypedHttpClient>(m_Builder);
        var serviceProvider = m_Services.BuildServiceProvider();
        var typedClient = serviceProvider.GetRequiredService<AbsolutePathTypedHttpClient>();

        await typedClient.Get();
        m_Builder.Reset();

        // When
        await typedClient.Get();

        // Then
        m_MatchingTracker.WasCalled(1);
    }

    public class AbsolutePathTypedHttpClient(HttpClient httpClient)
    {
        public async Task Get()
        {
            await httpClient.GetAsync("http://unit.test/call");
        }
    }

    #endregion

    #region Refit Tests

    // Although core TeePee does not have a dependency on Refit, it's important core doesn't break Refit and
    // this needs to be found before core TeePee is released and TeePee.Refit gets the updated package.

    [Fact]
    public async Task RetainsState_AcrossMultipleRefitUsesOfBuilder()
    {
        // Given
        m_Services.AddRefitClient<RefitUsage.IApiService>();
        m_Services.AttachToRefitInterface<RefitUsage.IApiService>(m_Builder);
        var serviceProvider = m_Services.BuildServiceProvider();
        await serviceProvider.GetRequiredService<RefitUsage.IApiService>().Call();

        // When
        await serviceProvider.GetRequiredService<RefitUsage.IApiService>().Call();

        // Then
        m_MatchingTracker.WasCalled(2);
    }

    [Fact]
    public async Task Reset_ClearsState_AcrossMultipleRefitUsesOfBuilder()
    {
        // Given
        m_Services.AddRefitClient<RefitUsage.IApiService>();
        m_Services.AttachToRefitInterface<RefitUsage.IApiService>(m_Builder);
        var serviceProvider = m_Services.BuildServiceProvider();

        await serviceProvider.GetRequiredService<RefitUsage.IApiService>().Call();
        m_Builder.Reset();

        // When
        await serviceProvider.GetRequiredService<RefitUsage.IApiService>().Call();

        // Then
        m_MatchingTracker.WasCalled(1);
    }

    [Fact]
    public async Task Reset_ClearsState_ForSameRefitUseOfBuilder()
    {
        // Given
        m_Services.AddRefitClient<RefitUsage.IApiService>();
        m_Services.AttachToRefitInterface<RefitUsage.IApiService>(m_Builder);
        var serviceProvider = m_Services.BuildServiceProvider();
        var apiService = serviceProvider.GetRequiredService<RefitUsage.IApiService>();

        await apiService.Call();
        m_Builder.Reset();

        // When
        await apiService.Call();

        // Then
        m_MatchingTracker.WasCalled(1);
    }

    #endregion
}

public static class RefitUsage
{
    public interface IApiService
    {
        [Get("/call")]
        Task<HttpResponseMessage> Call();
    }

    public static IServiceCollection AttachToRefitInterface<TRefitInterface>(this IServiceCollection serviceCollection, TeePeeBuilder teePeeBuilder)
        where TRefitInterface : class
    {
        serviceCollection
            .AddRefitClient<TRefitInterface>() // This should continue configuring the same Refit client
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("http://unit.test"))
            .AddSingletonTeePeeMessageHandler(teePeeBuilder);

        return serviceCollection;
    }
}
