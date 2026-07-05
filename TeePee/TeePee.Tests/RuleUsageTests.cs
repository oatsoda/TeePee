using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TeePee.Tests.TestData;

namespace TeePee.Tests;

/// <summary>
/// Tests to ensure the Handler is behaving correctly.
/// </summary>
public class RuleUsageTests
{
    private readonly ITestOutputHelper m_TestOutputHelper;

    // URL and Method used for each test
    private string m_Url = "https://www.test.co.uk/api/items";
    private HttpMethod m_HttpMethod = HttpMethod.Get;

    // Logger
    private readonly Mock<ILogger> m_MockLogger = new();

    // Instance of Tracking Builder for each test
    private TeePeeBuilder m_Builder;

    // Shortcut methods
    private RequestMatchBuilder RequestMatchBuilder() => m_Builder.ForRequest(m_Url, m_HttpMethod);

    private HttpRequestMessage RequestMessage() => RequestMessage(m_HttpMethod, m_Url);
    private static HttpRequestMessage RequestMessage(HttpMethod httpMethod, string url) => new(httpMethod, url);

    // NOTE: Using Manual() as SUT is the resultant HttpClient, not the injection.
    private Task<HttpClient> CreateHttpClient() => Task.FromResult(m_Builder.Manual().CreateClient());

    public RuleUsageTests(ITestOutputHelper testOutputHelper)
    {
        m_TestOutputHelper = testOutputHelper;

        m_MockLogger
            .Setup(l => l.IsEnabled(It.Is<LogLevel>(level => level >= LogLevel.Information)))
            .Returns(true);

        m_MockLogger
            .Setup(l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
                {
                    var logLevel = (LogLevel)invocation.Arguments[0];
                    var state = invocation.Arguments[2];
                    var exception = (Exception)invocation.Arguments[3];
                    var formatter = invocation.Arguments[4];
                    var invokeMethod = formatter.GetType().GetMethod("Invoke");
                    var logMessage = (string?)invokeMethod?.Invoke(formatter, new[] { state, exception });
                    testOutputHelper.WriteLine($"[{logLevel}] {logMessage}");
                }));

        m_Builder = new(opt => opt.Logger = m_MockLogger.Object);
    }

    #region Matches

    #region JSON Body

    public class BodyMatchTestData : TheoryData<string, Encoding, bool>
    {
        public BodyMatchTestData()
        {
            Add("text/plain", Encoding.UTF8, true);
            Add("text/plain", Encoding.UTF8, false);
            Add("application/json", Encoding.UTF8, true);
            Add("application/json", Encoding.UTF8, false);
            Add("text/plain", Encoding.ASCII, true);
            Add("text/plain", Encoding.ASCII, false);
        }
    }

    internal record BodyTypeForPartialMatch(int Test, object[] Other);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MatchesBodyWithDefaultEncodingAndMediaType(bool partialMatch)
    {
        // Given
        var bodyObject = new { Test = 1, Other = new[] { new { Thing = "Yes" }, new { Thing = "No" } } };
        var verify = partialMatch
                         ? RequestMatchBuilder().ThatHasBodyContaining<BodyTypeForPartialMatch>(b => b.Test == 1 && b.Other.Length == 2).TrackRequest()
                         : RequestMatchBuilder().ThatHasBody(bodyObject).TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(bodyObject), Encoding.UTF8, "application/json");

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasCalled();
    }

    [Theory]
    [ClassData(typeof(BodyMatchTestData))]
    public async Task MatchesBodyWithEncodingAndMediaType(string mediaType, Encoding encoding, bool partialMatch)
    {
        // Given
        var bodyObject = new { Test = 1, Other = new[] { new { Thing = "Yes" }, new { Thing = "No" } } };
        var verify = partialMatch
                         ? RequestMatchBuilder().ThatHasBodyContaining<BodyTypeForPartialMatch>(b => b.Test == 1 && b.Other.Length == 2, mediaType, encoding).TrackRequest()
                         : RequestMatchBuilder().ThatHasBody(bodyObject, mediaType, encoding).TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(bodyObject), encoding, mediaType);

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasCalled();
    }

    [Theory]
    [ClassData(typeof(BodyMatchTestData))]
    public async Task DoesNotMatchBodyWithDifferentSerialisationSettings(string mediaType, Encoding encoding, bool partialMatch)
    {
        // Given
        m_Builder = new(opt =>
                                {
                                    opt.CaseSensitiveMatching = true;
                                    opt.RequestBodySerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                                });
        var bodyObject = new { Test = 1, Other = new[] { new { Thing = "Yes" }, new { Thing = "No" } } };
        var verify = partialMatch
                         ? RequestMatchBuilder().ThatHasBodyContaining<BodyTypeForPartialMatch>(b => b.Test == 1 && b.Other.Length == 2, mediaType, encoding).TrackRequest()
                         : RequestMatchBuilder().ThatHasBody(bodyObject, mediaType, encoding).TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(bodyObject), encoding, mediaType);

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasNotCalled();
    }

    [Theory]
    [ClassData(typeof(BodyMatchTestData))]
    public async Task MatchesBodyWithSameSerialisationSettings(string mediaType, Encoding encoding, bool partialMatch)
    {
        // Given
        m_Builder = new(opt =>
                                {
                                    opt.CaseSensitiveMatching = true;
                                    opt.RequestBodySerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                                });
        var bodyObject = new { Test = 1, Other = new[] { new { Thing = "Yes" }, new { Thing = "No" } } };
        var verify = partialMatch
                         ? RequestMatchBuilder().ThatHasBodyContaining<BodyTypeForPartialMatch>(b => b.Test == 1 && b.Other.Length == 2, mediaType, encoding).TrackRequest()
                         : RequestMatchBuilder().ThatHasBody(bodyObject, mediaType, encoding).TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(bodyObject, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), encoding, mediaType);

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasCalled();
    }

    [Theory]
    [ClassData(typeof(BodyMatchTestData))]
    public async Task DoesNotMatchBodyIfMediaTypeDifferent(string mediaType, Encoding encoding, bool partialMatch)
    {
        // Given
        var bodyObject = new { Test = 1, Other = new[] { new { Thing = "Yes" }, new { Thing = "No" } } };
        var verify = partialMatch
                         ? RequestMatchBuilder().ThatHasBodyContaining<BodyTypeForPartialMatch>(b => b.Test == 1 && b.Other.Length == 2, mediaType, encoding).TrackRequest()
                         : RequestMatchBuilder().ThatHasBody(bodyObject, mediaType, encoding).TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(bodyObject), encoding, "wrong/media-type");

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasNotCalled();
    }

    [Theory]
    [ClassData(typeof(BodyMatchTestData))]
    public async Task DoesNotMatchBodyIfContentTypeDifferent(string mediaType, Encoding encoding, bool partialMatch)
    {
        // Given
        var bodyObject = new { Test = 1, Other = new[] { new { Thing = "Yes" }, new { Thing = "No" } } };
        var verify = partialMatch
                         ? RequestMatchBuilder().ThatHasBodyContaining<BodyTypeForPartialMatch>(b => b.Test == 1 && b.Other.Length == 2, mediaType, encoding).TrackRequest()
                         : RequestMatchBuilder().ThatHasBody(bodyObject, mediaType, encoding).TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(bodyObject), encoding.WebName == Encoding.UTF32.WebName ? Encoding.Latin1 : Encoding.UTF32, mediaType);

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasNotCalled();
    }

    private class ReferenceBodyType
    {
        public int Test { get; set; }
    }

    [Fact]
    public async Task MatchesBodyIfReferenceTypeAndAlteredAfterAssigning()
    {
        // Given
        var bodyObject = new ReferenceBodyType { Test = 1 };
        var verify = RequestMatchBuilder().ThatHasBody(bodyObject)
                                          .TrackRequest();

        bodyObject.Test = 23;

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(new { Test = 23 }), Encoding.UTF8, "application/json");

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasCalled();
    }

    #endregion

    #region Non-JSON Body

    [Theory]
    [ClassData(typeof(NonJsonContentTypesData))]
    public async Task MatchesNonJsonBody(HttpContent requestBodyContent)
    {
        // Given
        var verify = RequestMatchBuilder().ThatHasHttpContentBody(requestBodyContent)
                                          .TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = requestBodyContent;

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasCalled();
    }

    [Fact]
    public async Task DoesNotMatchIfNonJsonBodyWrongContentType()
    {
        // Given
        var expectedBody = new ByteArrayContent(new byte[] { 65, 98, 48 })
        {
            Headers = { ContentType = new("test/input") }
        };
        var verify = RequestMatchBuilder().ThatHasHttpContentBody(expectedBody)
                                          .TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new ByteArrayContent(new byte[] { 65, 98, 48 });

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasNotCalled();
    }

    [Fact]
    public async Task DoesNotMatchIfNonJsonBodyWrongEncoding()
    {
        // Given
        var expectedBody = new ByteArrayContent(new byte[] { 65, 98, 48 })
        {
            Headers = { ContentType = new("test/input") { CharSet = Encoding.UTF8.WebName } }
        };
        var verify = RequestMatchBuilder().ThatHasHttpContentBody(expectedBody)
                                          .TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new ByteArrayContent(new byte[] { 65, 98, 48 })
        {
            Headers = { ContentType = new("test/input") { CharSet = Encoding.ASCII.WebName } }
        };

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasNotCalled();
    }

    #endregion

    #region Query String/Params

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task MatchesQueryStringInUrl(HttpMethod httpMethod)
    {
        // Given
        m_Url = "https://www.test.co.uk/api/items?thing=value";
        m_HttpMethod = httpMethod;
        var verify = RequestMatchBuilder().TrackRequest();

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(RequestMessage(m_HttpMethod, m_Url), TestCt);

        // Then
        verify.WasCalled();
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task MatchesQueryParamsIfAllMatch(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        var verify = RequestMatchBuilder().ThatContainsQueryParam("name1", "val1")
                                          .ThatContainsQueryParam("name2", "val2")
                                          .TrackRequest();

        var httpRequestMessage = RequestMessage(m_HttpMethod, $"{m_Url}?Name1=val1&name2=VAL2&name3=val3");

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasCalled();
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task DoesNotMatchQueryParamsIfNotAllMatched(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        var verify = RequestMatchBuilder().ThatContainsQueryParam("name1", "val1")
                                          .ThatContainsQueryParam("name2", "val2")
                                          .TrackRequest();

        var httpRequestMessage = RequestMessage(m_HttpMethod, $"{m_Url}?Name1=val1&name3=val3");

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasNotCalled();
    }

    #endregion

    #region Headers

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task MatchesHeadersIfAllMatch(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        var verify = RequestMatchBuilder().ThatContainsHeader("name1", "val1")
                                          .ThatContainsHeader("name2", "val2")
                                          .TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Headers.Add("Name1", "val1");
        httpRequestMessage.Headers.Add("name2", "VAL2");
        httpRequestMessage.Headers.Add("name3", "val3");

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasCalled();
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task DoesNotMatchHeadersIfNotAllMatched(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        var verify = RequestMatchBuilder().ThatContainsHeader("name1", "val1")
                                          .ThatContainsHeader("name2", "val2")
                                          .TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Headers.Add("name2", "val2");
        httpRequestMessage.Headers.Add("name3", "val3");

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasNotCalled();
    }

    [Fact]
    public async Task MatchesHeadersIfOneValueOfHeaderMatches()
    {
        // Given
        var verify = RequestMatchBuilder().ThatContainsHeader("name1", "val1")
                                          .TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Headers.Add("Name1", new[] { "val1", "otherVal" });

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verify.WasCalled();
    }

    #endregion

    #region Rule Order/Specificity

    [Fact]
    public async Task MatchesMoreSpecificRequest()
    {
        // Given
        var bodyObject = new { Test = 1 };
        var verifyUrlOnly = RequestMatchBuilder().TrackRequest();
        var verifyUrlAndBody = RequestMatchBuilder()
                              .ThatHasBody(bodyObject)
                              .TrackRequest();
        var verifyUrlAndBodyAndOneQueryParam = RequestMatchBuilder()
                                              .ThatHasBody(bodyObject)
                                              .ThatContainsQueryParam("filter", "a")
                                              .TrackRequest();
        var verifyUrlAndBodyAndQueryParams = RequestMatchBuilder()
                                            .ThatHasBody(bodyObject)
                                            .ThatContainsQueryParam("filter", "a")
                                            .ThatContainsQueryParam("sort", "desc")
                                            .TrackRequest();
        var verifyUrlAndBodyAndQueryParamsAndOneHeader = RequestMatchBuilder()
                                                        .ThatHasBody(bodyObject)
                                                        .ThatContainsQueryParam("filter", "a")
                                                        .ThatContainsQueryParam("sort", "desc")
                                                        .ThatContainsHeader("h1", "v1")
                                                        .TrackRequest();
        var verifyUrlAndBodyAndQueryParamsAndHeaders = RequestMatchBuilder()
                                                      .ThatHasBody(bodyObject)
                                                      .ThatContainsQueryParam("filter", "a")
                                                      .ThatContainsQueryParam("sort", "desc")
                                                      .ThatContainsHeader("h1", "v1")
                                                      .ThatContainsHeader("h2", "v2")
                                                      .TrackRequest();

        var httpRequestMessage = RequestMessage(HttpMethod.Get, $"{m_Url}?filter=a&sort=desc");
        httpRequestMessage.Headers.Add("h1", "v1");
        httpRequestMessage.Headers.Add("h2", "v2");
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(bodyObject), Encoding.UTF8, "application/json");

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verifyUrlOnly.WasNotCalled();
        verifyUrlAndBody.WasNotCalled();
        verifyUrlAndBodyAndOneQueryParam.WasNotCalled();
        verifyUrlAndBodyAndQueryParams.WasNotCalled();
        verifyUrlAndBodyAndQueryParamsAndOneHeader.WasNotCalled();
        verifyUrlAndBodyAndQueryParamsAndHeaders.WasCalled();
    }

    [Fact]
    public async Task MatchesMostRecentRuleIfMultipleSameRules()
    {
        // Given
        var bodyObject = new { Test = 1 };
        var verifyUrlOne = RequestMatchBuilder().ThatHasBody(bodyObject).TrackRequest();
        var verifyUrlTwo = RequestMatchBuilder().ThatHasBody(bodyObject).TrackRequest();

        var httpRequestMessage = RequestMessage();
        httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(bodyObject), Encoding.UTF8, "application/json");

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // Then
        verifyUrlOne.WasNotCalled();
        verifyUrlTwo.WasCalled();
    }

    #endregion

    #region Match Logging

    [Fact]
    public async Task DoesNotLogMessageIfNoLogger()
    {
        // Given
        m_Builder = new TeePeeBuilder(); // Not setting Logger in Options.
        RequestMatchBuilder();
        using var client = await CreateHttpClient();

        // When
        await client.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.Empty(m_MockLogger.Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LogsMessage(bool isMatch)
    {
        // Given
        RequestMatchBuilder();
        if (!isMatch)
            m_HttpMethod = HttpMethod.Options;

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        m_MockLogger.Verify(l => l.Log(
            It.Is<LogLevel>(level => level == (isMatch ? LogLevel.Information : LogLevel.Warning)),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) =>
                                    o != null &&
                                    (o.ToString() ?? "").Contains($"{(isMatch ? "Matched" : "Unmatched")} Http request") &&
                                    (o.ToString() ?? "").Contains($"{m_HttpMethod} https://www.test.co.uk/api/items [H: ] [CE: ] [CT: ] [B: ] [Matched: {isMatch}]")
                                ),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>())
        , Times.Once);
    }

    [Fact]
    public async Task LogsFullDetailsMessageIfNotMatchAndSettingEnabled()
    {
        // Given
        m_Builder = new(opt =>
        {
            opt.Logger = m_MockLogger.Object;
            opt.ShowFullDetailsOnMatchFailure = true;
        });
        RequestMatchBuilder();
        m_HttpMethod = HttpMethod.Options;
        m_Builder.ForRequest("https://www.test.co.uk/api/items2", HttpMethod.Head);

        var httpClient = await CreateHttpClient();

        // When
        await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        m_MockLogger.Verify(l => l.Log(
            It.Is<LogLevel>(level => level == LogLevel.Warning),
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) =>
                                    o != null &&
                                    (o.ToString() ?? "").Contains("Unmatched Http request") &&
                                    (o.ToString() ?? "").Contains("OPTIONS https://www.test.co.uk/api/items [H: ] [CE: ] [CT: ] [B: ] [Matched: False]") &&
                                    (o.ToString() ?? "").Contains("\tHEAD https://www.test.co.uk/api/items2 [Q: ] [H: ] [CE: ] [CT: ] [B: ]\r\n\tGET https://www.test.co.uk/api/items [Q: ] [H: ] [CE: ] [CT: ] [B: ]")
                                ),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>())
        , Times.Once);
    }

    #endregion

    #endregion

    #region Tracker Specific

    [Theory]
    [InlineData(false, 1, null, "to be called at least once but was never called")]
    [InlineData(false, 1, 1, "to be called exactly 1 times but was called 0 times")]
    [InlineData(false, 1, 2, "to be called exactly 2 times but was called 0 times")]
    [InlineData(true, 1, 2, "to be called exactly 2 times but was called 1 times")]
    [InlineData(true, 2, 3, "to be called exactly 3 times but was called 2 times")]
    public async Task TrackerThrowsIfMatchNotMade(bool requestCorrectMatch, int callTimes, int? expectedCallTimes, string expectedExceptionMessageFragment)
    {
        // Given
        var verify = RequestMatchBuilder().TrackRequest();
        var httpClient = await CreateHttpClient();
        for (var i = 0; i < callTimes; i++)
        {
            var httpRequestMessage = RequestMessage(requestCorrectMatch ? HttpMethod.Get : HttpMethod.Put, m_Url);
            await httpClient.SendAsync(httpRequestMessage, TestCt);
        }

        // When
        void Verify() => verify.WasCalled(expectedCallTimes);

        // Then
        var ex = Assert.Throws<MismatchedTrackerExpectedCalls>(Verify);
        m_TestOutputHelper.WriteLine(ex.Message);
        Assert.Contains($"Expected {m_HttpMethod} {m_Url}", ex.Message);
        Assert.Contains(expectedExceptionMessageFragment, ex.Message);
        Assert.Contains("\tGET https://www.test.co.uk/api/items [Q: ] [H: ] [CE: ] [CT: ] [B: ]\r\n", ex.Message);
        Assert.Contains($"\t{(requestCorrectMatch ? HttpMethod.Get : HttpMethod.Put)} https://www.test.co.uk/api/items [H: ] [CE: ] [CT: ] [B: ] [Matched: {requestCorrectMatch}]", ex.Message);
        Assert.Same(verify, ex.Tracker);
    }

    [Fact]
    public async Task TrackerDoesNotThrowIfMatchMade()
    {
        // Given
        var verify = RequestMatchBuilder().TrackRequest();
        var httpRequestMessage = RequestMessage(HttpMethod.Put, m_Url);

        var httpClient = await CreateHttpClient();
        await httpClient.SendAsync(httpRequestMessage, TestCt);

        // When
        void Verify() => verify.WasNotCalled();

        // Then
        var ex = Record.Exception(Verify);
        Assert.Null(ex);
    }

    // TODO: What does this prove now?
    [Fact]
    public async Task TrackerHasCorrectCallsIfBuilderIsReset()
    {
        // Given
        var verify = RequestMatchBuilder().TrackRequest();
        var httpClient = await CreateHttpClient();
        await httpClient.SendAsync(RequestMessage(), TestCt);
        m_Builder.Reset();

        // When
        httpClient = await CreateHttpClient();
        await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        //Assert.Equal(1, verify.AllCalls.Count);
        //Assert.Equal(1, verify.MatchedCalls.Count);
        Assert.Single(verify.AllCalls);
        Assert.Single(verify.MatchedCalls);

        verify.WasCalled(1);
    }

    #endregion

    #region Responds With

    [Fact]
    public async Task ThrowsIfNoMatchInStrictMode()
    {
        // Given
        m_Builder = new(opt => opt.Mode = TeePeeMode.Strict);

        var httpClient = await CreateHttpClient();

        // When
        var ex = await Record.ExceptionAsync(async () => await httpClient.SendAsync(RequestMessage(), TestCt));

        // Then
        Assert.NotNull(ex);
        var nex = Assert.IsType<NotSupportedException>(ex);
        Assert.Contains("Unmatched Http request: GET https://www.test.co.uk/api/items", nex.Message);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithDefaultResponseIfNoMatch(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("EmptyContent", response.Content.GetType().Name);
        Assert.Empty(response.Headers);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithCustomDefaultResponseIfNoMatch(HttpMethod httpMethod)
    {
        // Given
        m_Builder.WithDefaultResponse(HttpStatusCode.BadGateway, "--bad-gateway--");
        m_HttpMethod = httpMethod;

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        Assert.NotNull(response.Content);
        var body = await response.Content.ReadAsStringAsync(TestCt);
        Assert.Equal("--bad-gateway--", body);
        Assert.Empty(response.Headers);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithDefaultResponseIfNoResponseConfigured(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        RequestMatchBuilder();

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("EmptyContent", response.Content.GetType().Name);
        Assert.Empty(response.Headers);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithDefaultStatusIfResponseConfiguredWithoutStatus(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        RequestMatchBuilder().Responds();

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithCorrectStatus(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        RequestMatchBuilder().Responds()
                             .WithStatus(HttpStatusCode.InternalServerError);

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithCorrectBodyIfNoContentTypeConfigured(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        var bodyObject = new { Test = 1, Other = new[] { new { Thing = "Yes" }, new { Thing = "No" } } };
        RequestMatchBuilder().Responds()
                             .WithBody(bodyObject);

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        var responseBody = await response.Content.ReadAsStringAsync(TestCt);
        Assert.Equal(JsonSerializer.Serialize(bodyObject), responseBody);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
    }

    [Theory]
    [ClassData(typeof(JsonContentTypesData))]
    public async Task RespondsWithCorrectBody(string mediaType, Encoding encoding)
    {
        // Given
        var bodyObject = new { Test = 1, Other = new[] { new { Thing = "Yes" }, new { Thing = "No" } }, EnumVal = ToTestJsonSettings.Off };
        RequestMatchBuilder().Responds()
                             .WithBody(bodyObject, mediaType, encoding);

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        var responseBody = await response.Content.ReadAsStringAsync(TestCt);
        Assert.Equal(JsonSerializer.Serialize(bodyObject, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }), responseBody);
        Assert.Equal(mediaType, response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(encoding.WebName, response.Content.Headers.ContentType.CharSet);
    }

    [Theory]
    [ClassData(typeof(NonJsonContentTypesData))]
    public async Task RespondsWithCorrectNonJsonBody(HttpContent httpContent)
    {
        // Given
        RequestMatchBuilder().Responds()
                             .WithHttpContentBody(httpContent);

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // The
        Assert.NotNull(response);
        Assert.Equal(httpContent.GetType(), response.Content.GetType());
        Assert.Equal(httpContent.Headers.ContentType?.MediaType, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(httpContent.Headers.ContentType?.CharSet, response.Content.Headers.ContentType?.CharSet);
    }

    [Theory]
    [ClassData(typeof(JsonContentTypesData))]
    public async Task RespondsWithCorrectBodyIfSameClientUsedAndResponseDisposed(string mediaType, Encoding encoding)
    {
        // Given
        var bodyObject = new { Test = 1 };
        RequestMatchBuilder().Responds()
                             .WithBody(bodyObject, mediaType, encoding);

        var httpClient = await CreateHttpClient();

        var firstResponse = await httpClient.SendAsync(RequestMessage(), TestCt);
        firstResponse.Dispose();

        var secondResponse = await httpClient.SendAsync(RequestMessage(), TestCt);

        // When
        var responseBody = await secondResponse.Content.ReadAsStringAsync(TestCt);

        // Then
        Assert.Equal(JsonSerializer.Serialize(bodyObject, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }), responseBody);
        Assert.Equal(mediaType, secondResponse.Content.Headers.ContentType!.MediaType);
        Assert.Equal(encoding.WebName, secondResponse.Content.Headers.ContentType.CharSet);
    }

    [Fact]
    public async Task RespondsWithCorrectBodyIfReferenceTypeAndAlteredAfterAssigning()
    {
        // Given
        var bodyObject = new ReferenceBodyType { Test = 1 };
        RequestMatchBuilder().Responds()
                             .WithBody(bodyObject);

        bodyObject.Test = 23;

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        var responseBody = await response.Content.ReadAsStringAsync(TestCt);
        Assert.Equal(JsonSerializer.Serialize(new { Test = 23 }, new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }), responseBody);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithCorrectHeaders(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        RequestMatchBuilder().Responds()
                             .WithHeader("Set-Cookie", ".aspnetcookie=123");

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        var (key, values) = Assert.Single(response.Headers);
        Assert.Equal("Set-Cookie", key);
        var headerValue = Assert.Single(values);
        Assert.Equal(".aspnetcookie=123", headerValue);
    }

    [Fact]
    public async Task RespondsWithCorrectBodyWithDefaultJsonSerializerOptions()
    {
        // Given
        var bodyObject = new { Nullable = (string?)null, Case = "value", EnumVal = ToTestJsonSettings.Off };
        RequestMatchBuilder().Responds()
                             .WithBody(bodyObject);

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        var responseBody = await response.Content.ReadAsStringAsync(TestCt);
        Assert.Equal("{\"Nullable\":null,\"Case\":\"value\",\"EnumVal\":\"Off\"}", responseBody);
    }

    [Fact]
    public async Task RespondsWithCorrectBodyWithCustomJsonSerializerOptions()
    {
        // Given
        var jsonSerializeOptions = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        m_Builder = new(opt => opt.ResponseBodySerializerOptions = jsonSerializeOptions);
        var bodyObject = new { Nullable = (string?)null, Case = "value", EnumVal = ToTestJsonSettings.Off };
        RequestMatchBuilder().Responds()
                             .WithBody(bodyObject);

        var httpClient = await CreateHttpClient();

        // When
        var response = await httpClient.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.NotNull(response);
        var responseBody = await response.Content.ReadAsStringAsync(TestCt);
        Assert.Equal("{\"case\":\"value\",\"enumVal\":3}", responseBody);
    }

    private enum ToTestJsonSettings
    {
        On = 2,
        Off = 3
    }

    #region Chained Responses

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithSameResponseStatusIfNoChainedResponse(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        RequestMatchBuilder()
           .Responds()
           .WithStatus(HttpStatusCode.Ambiguous);

        using var client = await CreateHttpClient();
        var firstResponse = await client.SendAsync(RequestMessage(), TestCt);
        Assert.Equal(HttpStatusCode.Ambiguous, firstResponse.StatusCode);

        // When
        var secondResponse = await client.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.Equal(HttpStatusCode.Ambiguous, secondResponse.StatusCode);
    }

    [Fact]
    public async Task RespondsWithLastResponseIfChainedResponseConfiguredAndExceedsNumberOfChainedResponses()
    {
        // Given
        RequestMatchBuilder()
           .Responds()
           .WithStatus(HttpStatusCode.Ambiguous)
           .ThenResponds()
           .WithStatus(HttpStatusCode.ExpectationFailed);

        using var client = await CreateHttpClient();
        await client.SendAsync(RequestMessage(), TestCt);
        await client.SendAsync(RequestMessage(), TestCt);

        // When
        var thirdResponse = await client.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.Equal(HttpStatusCode.ExpectationFailed, thirdResponse.StatusCode);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithChainedStatusIfChainedResponseConfigured(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        RequestMatchBuilder()
           .Responds()
           .WithStatus(HttpStatusCode.Ambiguous)
           .ThenResponds()
           .WithStatus(HttpStatusCode.ExpectationFailed);

        using var client = await CreateHttpClient();

        var firstResponse = await client.SendAsync(RequestMessage(), TestCt);
        Assert.Equal(HttpStatusCode.Ambiguous, firstResponse.StatusCode);

        // When
        var secondResponse = await client.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.Equal(HttpStatusCode.ExpectationFailed, secondResponse.StatusCode);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithStatusIfMultipleChainedResponseConfigured(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        RequestMatchBuilder()
           .Responds()
           .WithStatus(HttpStatusCode.Ambiguous)
           .ThenResponds()
           .WithStatus(HttpStatusCode.ExpectationFailed)
           .ThenResponds()
           .WithStatus(HttpStatusCode.MisdirectedRequest);

        using var client = await CreateHttpClient();

        var firstResponse = await client.SendAsync(RequestMessage(), TestCt);
        Assert.Equal(HttpStatusCode.Ambiguous, firstResponse.StatusCode);

        var secondResponse = await client.SendAsync(RequestMessage(), TestCt);
        Assert.Equal(HttpStatusCode.ExpectationFailed, secondResponse.StatusCode);

        // When
        var thirdResponse = await client.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.Equal(HttpStatusCode.MisdirectedRequest, thirdResponse.StatusCode);
    }

    [Theory]
    [ClassData(typeof(CommonHttpMethodsData))]
    public async Task RespondsWithDefaultStatusIfChainedResponseConfiguredWithoutStatus(HttpMethod httpMethod)
    {
        // Given
        m_HttpMethod = httpMethod;
        RequestMatchBuilder()
           .Responds()
           .WithStatus(HttpStatusCode.Ambiguous)
           .ThenResponds();

        using var client = await CreateHttpClient();

        await client.SendAsync(RequestMessage(), TestCt);

        // When
        var secondResponse = await client.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.Equal(HttpStatusCode.NoContent, secondResponse.StatusCode);
    }

    [Fact]
    public async Task TrackerHasCorrectCallsIfChainedResponses()
    {
        // Given
        var verify = RequestMatchBuilder()
                    .Responds()
                    .WithStatus(HttpStatusCode.BadRequest)
                    .ThenResponds()
                    .WithStatus(HttpStatusCode.Accepted)
                    .ThenResponds()
                    .TrackRequest();

        using var client = await CreateHttpClient();
        await client.SendAsync(RequestMessage(), TestCt);
        await client.SendAsync(RequestMessage(), TestCt);

        // When
        await client.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.Equal(3, verify.AllCalls.Count);
        Assert.Equal(3, verify.MatchedCalls.Count);

        verify.WasCalled(3);

        Assert.Equal(HttpStatusCode.BadRequest, verify.MatchedCalls[0].Response.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, verify.MatchedCalls[1].Response.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, verify.MatchedCalls[2].Response.StatusCode);
    }

    [Fact]
    public async Task TrackerIsSameOnAnyChainedResponse()
    {
        // Given
        var firstResponseBuilder = RequestMatchBuilder()
                                  .Responds()
                                  .WithStatus(HttpStatusCode.BadRequest);
        var firstTracker = firstResponseBuilder.TrackRequest();

        var secondTracker = firstResponseBuilder
                           .ThenResponds()
                           .WithStatus(HttpStatusCode.Accepted)
                           .TrackRequest();

        using var client = await CreateHttpClient();
        await client.SendAsync(RequestMessage(), TestCt);

        // When
        await client.SendAsync(RequestMessage(), TestCt);

        // Then
        Assert.Same(firstTracker, secondTracker);
        firstTracker.WasCalled(2);
        secondTracker.WasCalled(2);
    }

    #endregion

    #endregion
}