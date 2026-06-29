using Microsoft.Extensions.DependencyInjection;

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
        await m_Builder.Manual().CreateClient().SendAsync(m_MatchingHttpRequestTwo, TestCt);

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
}
