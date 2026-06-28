using Microsoft.Extensions.DependencyInjection;

namespace TeePee.Tests;

/// <summary>
/// Tests for behaviours attaching TeePee to already registered HttpClient setups via DI.
/// </summary>
public class InjectionUsageTests
{
    private ServiceCollection m_Services;

    private readonly HttpRequestMessage m_MatchingHttpRequest;
    private readonly Tracker m_MatchingTracker;

    private readonly TeePeeBuilder m_Builder;

    public InjectionUsageTests()
    {
        m_Services = new ServiceCollection();
        m_Services.AddHttpClient();
        m_Services.AddHttpClient<AbsolutePathTypedHttpClient>();

        m_MatchingHttpRequest = new(HttpMethod.Get, "https://non-existant.none/api/items");

        m_Builder = new TeePeeBuilder();
        m_MatchingTracker = m_Builder
            .ForRequest(m_MatchingHttpRequest.RequestUri!.ToString(), m_MatchingHttpRequest.Method)
            .TrackRequest();
    }

    #region Default Client

    [Fact]
    public async Task AttachToDefaultClient_InterceptsRequests()
    {
        // Given
        m_Services.AttachToDefaultClient(m_Builder);
        var httpClientFactory = m_Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        // When
        await httpClientFactory.CreateClient().SendAsync(m_MatchingHttpRequest, TestCt);

        // Then
        m_MatchingTracker.WasCalled();
    }

    [Theory]
    [InlineData("https://non-existant.none", "api/items")]
    [InlineData("https://non-existant.none/", "api/items")]
    [InlineData("https://non-existant.none/", "/api/items")]
    [InlineData("https://non-existant.none", "/api/items")]
    public async Task AttachToDefaultClient_MatchesRelativePathsIfBaseUrlSupplied(string baseUrl, string requestUrl)
    {
        // Given
        m_Services = new();
        m_Services.ConfigureHttpClientDefaults(b => b.ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl)));
        m_Services.AttachToDefaultClient(m_Builder);
        var httpClientFactory = m_Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        var relativePathRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        // When
        await httpClientFactory.CreateClient().SendAsync(relativePathRequest, TestCt);

        // Then
        m_MatchingTracker.WasCalled();
    }

    #endregion

    #region Named Client

    [Theory]
    [InlineData("a", "a")]
    [InlineData(" b ", " b ")]
    [InlineData("myClient", "myClient")]
    public async Task AttachToNamedClient_InterceptsRequests(string? configuredName, string requestedName)
    {
        // Given
        m_Services.AttachToNamedClient(m_Builder, configuredName!);
        var httpClientFactory = m_Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        // When
        await httpClientFactory.CreateClient(requestedName).SendAsync(m_MatchingHttpRequest, TestCt);

        // Then
        m_MatchingTracker.WasCalled();
    }

    [Theory]
    [InlineData("a", "A")]
    [InlineData(" b ", "b")]
    [InlineData("c", " c ")]
    [InlineData("myClient", "wrongClient")]
    public async Task AttachToNamedClient_DoesNotInterceptRequests_IfWrongClientName(string? configuredName, string requestedName)
    {
        // Given
        m_Services.AttachToNamedClient(m_Builder, configuredName!);
        var httpClientFactory = m_Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

        // When
        var ex = await Record.ExceptionAsync(async () => await httpClientFactory.CreateClient(requestedName).SendAsync(m_MatchingHttpRequest, TestCt));

        // Then
        Assert.IsType<HttpRequestException>(ex);
        Assert.Contains("No such host is known", ex.Message);
        ex = Record.Exception(() => m_MatchingTracker.WasNotCalled());
        Assert.IsType<InvalidOperationException>(ex);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AttachToNamedClient_Throws_IfConfiguredWithoutClientName(string? configuredName)
    {
        // When
        var ex = Record.Exception(() => m_Services.AttachToNamedClient(m_Builder, configuredName!));

        // Then
        Assert.IsType<ArgumentException>(ex);
        Assert.Contains("Cannot attached to a Named client without a Name", ex.Message);
    }

    [Theory]
    [InlineData("https://non-existant.none", "api/items")]
    [InlineData("https://non-existant.none/", "api/items")]
    [InlineData("https://non-existant.none/", "/api/items")]
    [InlineData("https://non-existant.none", "/api/items")]
    public async Task AttachToNamedClient_MatchesRelativePathsIfBaseUrlSupplied(string baseUrl, string requestUrl)
    {
        // Given
        m_Services = new();
        m_Services.AddHttpClient("some-name", c => c.BaseAddress = new Uri(baseUrl));
        m_Services.AttachToNamedClient(m_Builder, "some-name");
        var httpClientFactory = m_Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();
        var relativePathRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        // When
        await httpClientFactory.CreateClient("some-name").SendAsync(relativePathRequest, TestCt);

        // Then
        m_MatchingTracker.WasCalled();
    }

    #endregion

    #region Typed Client

    [Fact]
    public async Task AttachToTypedClient_InterceptsRequests()
    {
        // Given
        m_Services.AttachToTypedClient<AbsolutePathTypedHttpClient>(m_Builder);
        var typedClient = m_Services.BuildServiceProvider().GetRequiredService<AbsolutePathTypedHttpClient>();

        // When
        await typedClient.Get();

        // Then
        m_MatchingTracker.WasCalled();
    }

    [Theory]
    [InlineData("https://non-existant.none")]
    [InlineData("https://non-existant.none/")]
    public async Task AttachToTypedClient_MatchesRelativePathsIfBaseUrlSupplied(string baseUrl)
    {
        // Given
        m_Services = new();
        m_Services.AddHttpClient<RelativePathTypedHttpClient>(c => c.BaseAddress = new Uri(baseUrl));
        m_Services.AttachToTypedClient<RelativePathTypedHttpClient>(m_Builder);
        var typedClient = m_Services.BuildServiceProvider().GetRequiredService<RelativePathTypedHttpClient>();

        // When
        await typedClient.Get();

        // Then
        m_MatchingTracker.WasCalled();
    }

    public class AbsolutePathTypedHttpClient(HttpClient httpClient)
    {
        public async Task Get()
        {
            await httpClient.GetAsync("https://non-existant.none/api/items");
        }
    }

    public class RelativePathTypedHttpClient(HttpClient httpClient)
    {
        public async Task Get()
        {
            await httpClient.GetAsync("/api/items");
        }
    }

    #endregion
}