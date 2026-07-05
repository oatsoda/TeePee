namespace TeePee.Tests;

/// <summary>
/// Tests for behaviours specific to Manual() usages.
/// </summary>
public class ManualUsageTests
{
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
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData("myClient", "myClient")]
    public async Task ManualCreateHttpClientFactoryMatchesIfNamedClientMatches(string configuredName, string requestedName)
    {
        // Given
        var httpClientFactory = m_Builder.Manual().CreateHttpClientFactory(configuredName);

        // When
        await httpClientFactory.CreateClient(requestedName).SendAsync(m_MatchingHttpRequest, TestCt);

        // Then
        m_MatchingTracker.WasCalled();
    }

    [Theory]
    [InlineData("", " ")]
    [InlineData("myClient", "wrongClient")]
    [InlineData("", "wrongClient")]
    [InlineData(" ", "")]
    public async Task ManualCreateHttpClientFactoryCreateClientThrowsIfNamedClientDoesNotMatch(string configuredName, string? requestedName)
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
        await httpClient.SendAsync(relativePathRequest, TestCt);

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
        await httpClientFactory.CreateClient().SendAsync(relativePathRequest, TestCt);

        // Then
        m_MatchingTracker.WasCalled();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData("myClient", "myClient")]
    public async Task MultipleManualToHttpClientFactoryMatchesIfNamedClientMatches(string configuredName, string requestedName)
    {
        // Given
        var builderTwo = new TeePeeBuilder();

        var httpClientFactory = new[] { (configuredName, m_Builder.Manual()), ("Second", builderTwo.Manual()) }.ToHttpClientFactory();

        // When
        await httpClientFactory.CreateClient(requestedName).SendAsync(m_MatchingHttpRequest, TestCt);

        // Then
        m_MatchingTracker.WasCalled();
    }

    [Theory]
    [InlineData("", " ")]
    [InlineData("myClient", "wrongClient")]
    [InlineData("", "wrongClient")]
    public async Task MultipleManualToHttpClientFactoryCreateClientThrowsIfNamedClientDoesNotMatch(string configuredName, string requestedName)
    {
        // Given
        var builderTwo = new TeePeeBuilder();

        var httpClientFactory = new[] { (configuredName, m_Builder.Manual()), ("Second", builderTwo.Manual()) }.ToHttpClientFactory();

        // When
        var ex = Record.Exception(() => httpClientFactory.CreateClient(requestedName));

        // Then
        Assert.IsType<ArgumentOutOfRangeException>(ex);
        Assert.Contains($"No HttpClients configured with name '{requestedName}'", ex.Message);
        Assert.Contains($"Configured with '{configuredName}', 'Second'", ex.Message);
    }
}