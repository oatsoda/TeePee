namespace TeePee.Tests;

/// <summary>
/// Tests for behaviours specific to Manual() usages.
/// </summary>
public class ManualUsageTests
{
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    private readonly HttpRequestMessage m_MatchingHttpRequest;
    private readonly Tracker m_MatchingTracker;

    private readonly TeePeeBuilder m_Builder;

    public ManualUsageTests()
    {
        m_MatchingHttpRequest = new(HttpMethod.Get, "https://testing.com/api/items");

        m_Builder = new TeePeeBuilder();
        m_MatchingTracker = m_Builder
            .ForRequest(m_MatchingHttpRequest.RequestUri!.ToString(), m_MatchingHttpRequest.Method)
            .TrackRequest();
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("myClient", "myClient")]
    public async Task ManualCreateHttpClientFactoryMatchesIfNamedClientMatches(string? configuredName, string requestedName)
    {
        // Given
        var httpClientFactory = m_Builder.Manual().CreateHttpClientFactory(configuredName);

        // When
        await httpClientFactory.CreateClient(requestedName).SendAsync(m_MatchingHttpRequest, CancellationToken);

        // Then
        m_MatchingTracker.WasCalled();
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("myClient", "wrongClient")]
    [InlineData(null, "wrongClient")]
    public async Task ManualCreateHttpClientFactoryCreateClientThrowsIfNamedClientDoesNotMatch(string? configuredName, string? requestedName)
    {
        // Given
        var httpClientFactory = m_Builder.Manual().CreateHttpClientFactory(configuredName);

        // When
        var ex = Record.Exception(() => httpClientFactory.CreateClient(requestedName!));

        // Then
        Assert.IsType<ArgumentOutOfRangeException>(ex);
        Assert.Contains($"No HttpClients configured with name '{requestedName}'. Configured with '{configuredName}'", ex.Message);
    }

    [Theory]
    [InlineData("https://testing.com", "api/items")]
    [InlineData("https://testing.com/", "api/items")]
    [InlineData("https://testing.com/", "/api/items")]
    [InlineData("https://testing.com", "/api/items")]
    public async Task ManualCreateClientMatchesRelativePathsIfBaseUrlSupplied(string baseUrl, string requestUrl)
    {
        // Given
        var httpClient = m_Builder.Manual(baseUrl).CreateClient();
        var relativePathRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        // When
        await httpClient.SendAsync(relativePathRequest, CancellationToken);

        // Then
        m_MatchingTracker.WasCalled();
    }


    [Theory]
    [InlineData("https://testing.com", "api/items")]
    [InlineData("https://testing.com/", "api/items")]
    [InlineData("https://testing.com/", "/api/items")]
    [InlineData("https://testing.com", "/api/items")]
    public async Task ManualCreateClientFactoryMatchesRelativePathsIfBaseUrlSupplied(string baseUrl, string requestUrl)
    {
        // Given
        var httpClientFactory = m_Builder.Manual(baseUrl).CreateHttpClientFactory("");
        var relativePathRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);

        // When
        await httpClientFactory.CreateClient().SendAsync(relativePathRequest, CancellationToken);

        // Then
        m_MatchingTracker.WasCalled();
    }

    // TODO: Fix up multiple named clients stuff
    //[Theory]
    //[InlineData(null, "")]
    //[InlineData("", "")]
    //[InlineData("myClient", "myClient")]
    //public async Task MultipleManualToHttpClientFactoryMatchesIfNamedClientMatches(string? configuredName, string? requestedName)
    //{
    //    // Given
    //    var builderOne = configuredName == null ? new() : new TeePeeBuilder(configuredName);
    //    var builderTwo = new TeePeeBuilder("Second");

    //    var verify = builderOne.ForRequest(m_Url, m_HttpMethod).TrackRequest();

    //    var httpClientFactory = new[] { (await builderOne.Build()).Manual(), (await builderTwo.Build()).Manual() }.ToHttpClientFactory();

    //    // When
    //    await httpClientFactory.CreateClient(requestedName!).SendAsync(RequestMessage(), CancellationToken);

    //    // Then
    //    verify.WasCalled();
    //}

    // TODO: Probably not needed - will instead leave it to just fail the test because the mocking won't be hooked up to what's expected?
    //[Theory]
    //[InlineData(null, null)]
    //[InlineData("", null)]
    //[InlineData("myClient", "wrongClient")]
    //[InlineData(null, "wrongClient")]
    //public async Task MultipleManualToHttpClientFactoryCreateClientThrowsIfNamedClientDoesNotMatch(string? configuredName, string? requestedName)
    //{
    //    // Given
    //    var builderOne = configuredName == null ? new() : new TeePeeBuilder(configuredName);
    //    var builderTwo = new TeePeeBuilder("Second");

    //    var httpClientFactory = new[] { (await builderOne.Build()).Manual(), (await builderTwo.Build()).Manual() }.ToHttpClientFactory();

    //    // When
    //    var ex = Record.Exception(() => httpClientFactory.CreateClient(requestedName!));

    //    // Then
    //    Assert.IsType<ArgumentOutOfRangeException>(ex);
    //    Assert.Contains($"No HttpClients configured with name '{requestedName}'. Configured with '{configuredName}','Second'", ex.Message);
    //}
}