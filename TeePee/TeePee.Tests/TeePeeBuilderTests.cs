using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;

namespace TeePee.Tests;

/// <summary>
/// Tests to ensure validation and state of the Builder are correct.
/// Actual usage of this data is done in the TeePee tests where Http requests are made and assertions concluded.
/// </summary>
public class TeePeeBuilderTests
{
    private TeePeeBuilder m_Builder = new();

    #region ForRequest

    [Theory]
    [InlineData("/api/items")]
    [InlineData("www.site.com/api/items")]
    [InlineData("")]
    [InlineData(" ")]
    public void ForRequestThrowsIfNotAbsolute(string relativeUrl)
    {
        // Given
        // When
        var ex = Record.Exception(() => m_Builder.ForRequest(relativeUrl, HttpMethod.Get));

        // Then
        Assert.IsType<ArgumentException>(ex);
        Assert.Contains("must be an absolute URI", ex.Message);
    }
        
    [Theory]
    [InlineData("http://www.site.com/api/items")]
    [InlineData("https://site.com")]
    public void ForRequestDoesNotThrowIfAbsolute(string absoluteUrl)
    {
        // Given
        // When
        var ex = Record.Exception(() => m_Builder.ForRequest(absoluteUrl, HttpMethod.Get));

        // Then
        Assert.Null(ex);
    }
        
    public static IEnumerable<object[]> UrlAndMethodData()
    {
        yield return new object[] { "https://site.net/api.items", HttpMethod.Get };
        yield return new object[] { "https://site.net/api.items", HttpMethod.Post };
        yield return new object[] { "https://site.net/api.items", HttpMethod.Delete };
        yield return new object[] { "https://site.net/api.items", HttpMethod.Put };
        yield return new object[] { "https://site.net/api.items?filter", HttpMethod.Get };
        yield return new object[] { "https://www.site.com", HttpMethod.Get };
    }
        
    [Theory]
    [MemberData(nameof(UrlAndMethodData))]
    public void ForRequestDoesNotThrowOnDuplicateUrlAndMethod(string url, HttpMethod method)
    {
        // Given
        m_Builder.ForRequest(url, method);

        // When 
        var ex = Record.Exception(() => m_Builder.ForRequest(url, method));
            
        // Then
        Assert.Null(ex);
    }

    [Theory]
    [MemberData(nameof(UrlAndMethodData))]
    public void ForRequestThrowsOnDuplicateUrlAndMethodIfUniqueUrlsRequired(string url, HttpMethod method)
    {
        // Given
        m_Builder = new(opt => opt.BuilderMode = TeePeeBuilderMode.RequireUniqueUrlRules);
        m_Builder.ForRequest(url, method);

        // When 
        var ex = Record.Exception(() => m_Builder.ForRequest(url, method));
            
        // Then
        Assert.IsType<ArgumentException>(ex);
        Assert.Contains("already a request match for", ex.Message);
    }
        
    public static IEnumerable<object[]> UrlAndMethodDifferentCombinations()
    {
        yield return new object[]
                     {
                         "https://site.net/api.items", HttpMethod.Get,
                         "https://site.net/api.items", HttpMethod.Post
                     };
        yield return new object[]
                     {
                         "https://site.net/api.items", HttpMethod.Post,
                         "https://site.net/api.items", HttpMethod.Put
                     };
        yield return new object[]
                     {
                         "https://site.net/api.items", HttpMethod.Get,
                         "https://site.net/api.items", HttpMethod.Delete
                     };
        yield return new object[]
                     {
                         "https://site.net/api.items", HttpMethod.Get,
                         "https://site.net/api.items?", HttpMethod.Get
                     };
        yield return new object[]
                     {
                         "https://site.net/api.items", HttpMethod.Get,
                         "https://site.net/api.items?filter", HttpMethod.Get
                     };
        yield return new object[]
                     {
                         "https://site.net/api.items", HttpMethod.Get,
                         "https://site.net/api.items?filter=value", HttpMethod.Get
                     };
        yield return new object[]
                     {
                         "https://www.site.com", HttpMethod.Get,
                         "https://www.site.com/", HttpMethod.Get
                     };
        yield return new object[]
                     {
                         "https://www.site.com", HttpMethod.Get,
                         "https://www.site.com/api", HttpMethod.Get
                     };
        yield return new object[]
                     {
                         "https://www.site.com", HttpMethod.Get,
                         "http://www.site.com", HttpMethod.Get
                     };
        yield return new object[]
                     {
                         "http://www.site.com", HttpMethod.Get,
                         "http://site.com", HttpMethod.Get
                     };
        yield return new object[]
                     {
                         "https://www.site.com", HttpMethod.Get,
                         "https://www.site.co.uk", HttpMethod.Get
                     };
    }

    [Theory]
    [MemberData(nameof(UrlAndMethodDifferentCombinations))]
    public void ForRequestDoesNotThrowOnDifferentUrlAndMethod(string firstUrl, HttpMethod firstMethod,
                                                              string secondUrl, HttpMethod secondMethod)
    {
        // Given
        m_Builder = new(opt => opt.BuilderMode = TeePeeBuilderMode.RequireUniqueUrlRules);
        m_Builder.ForRequest(firstUrl, firstMethod);

        // When 
        var ex = Record.Exception(() => m_Builder.ForRequest(secondUrl, secondMethod));
            
        // Then
        Assert.Null(ex);
    }
    
    [Fact]
    public void ForRequestThrowsIfBuildAlreadyCalled()
    {
        // Given
        m_Builder.Build();

        // When 
        var ex = Record.Exception(() => m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get));
            
        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("Cannot add more request tracking", ex.Message);
    }

    #endregion

    #region ThatHasBody

    [Fact]
    public void ThatHasBodyThrowsIfCalledMoreThanOnce()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                      .ThatHasBody("test");

        // When
        var ex = Record.Exception(() => requestBuilder.ThatHasBody("test"));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("matching Body has already been added", ex.Message);
    }

    [Fact]
    public void ThatHasBodyThrowsIfWithHttpContentBodyAlreadyCalled()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                      .ThatHasHttpContentBody(new StringContent(""));

        // When
        var ex = Record.Exception(() => requestBuilder.ThatHasBody("test"));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("matching Body has already been added", ex.Message);
    }

    [Fact]
    public void ThatHasBodyThrowsIfWithBodyContainingAlreadyCalled()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                      .ThatHasBodyContaining<object>(_ => true);

        // When
        var ex = Record.Exception(() => requestBuilder.ThatHasBody("test"));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("matching Body has already been added", ex.Message);
    }
        
    #endregion

    #region ThatHasHttpContentBody

    [Fact]
    public void ThatHasHttpContentBodyThrowsIfCalledMoreThanOnce()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                      .ThatHasHttpContentBody(new StringContent(""));

        // When
        var ex = Record.Exception(() => requestBuilder.ThatHasHttpContentBody(new StringContent("")));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("matching Body has already been added", ex.Message);
    }

    [Fact]
    public void ThatHasHttpContentBodyThrowsIfWithBodyAlreadyCalled()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                      .ThatHasBody("test");

        // When
        var ex = Record.Exception(() => requestBuilder.ThatHasHttpContentBody(new StringContent("")));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("matching Body has already been added", ex.Message);
    }

    [Fact]
    public void ThatHasHttpContentBodyThrowsIfWithBodyContainingAlreadyCalled()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                      .ThatHasBodyContaining<object>(_ => true);

        // When
        var ex = Record.Exception(() => requestBuilder.ThatHasHttpContentBody(new StringContent("")));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("matching Body has already been added", ex.Message);
    }

    #endregion

    #region ThatHasBodyContaining
        
    [Fact]
    public void ThatHasBodyContainingThrowsIfCalledMoreThanOnce()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                      .ThatHasBodyContaining<object>(_ => true);

        // When
        var ex = Record.Exception(() => requestBuilder.ThatHasBodyContaining<object>(_ => true));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("matching Body has already been added", ex.Message);
    }

    [Fact]
    public void ThatHasBodyContainingThrowsIfWithBodyAlreadyCalled()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                      .ThatHasBody("test");

        // When
        var ex = Record.Exception(() => requestBuilder.ThatHasBodyContaining<object>(_ => true));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("matching Body has already been added", ex.Message);
    }

    [Fact]
    public void ThatHasBodyContainingThrowsIfWithHttpContentBodyAlreadyCalled()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                      .ThatHasHttpContentBody(new StringContent(""));

        // When
        var ex = Record.Exception(() => requestBuilder.ThatHasBodyContaining<object>(_ => true));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("matching Body has already been added", ex.Message);
    }

    #endregion

    #region ThatContainsQueryParam

    [Fact]
    public void ThatContainsQueryParamThrowsIfUrlHasQueryString()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/items?filter=value", HttpMethod.Get);

        // When
        var ex = Record.Exception(() => requestBuilder.ThatContainsQueryParam("sort", "name"));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("Url has already been configured to match with a QueryString", ex.Message);
    }
        
    [Fact]
    public void ThatContainsQueryParamThrowsIfAnotherMatchContainingQueryParamAlreadyExists()
    {
        // Given
        m_Builder.ForRequest("https://site.net/api/items?filter=value", HttpMethod.Get);
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/other", HttpMethod.Get);

        // When
        var ex = Record.Exception(() => requestBuilder.ThatContainsQueryParam("sort", "name"));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("request matches already exist with QueryString matching", ex.Message);
    }
        
    [Fact]
    public void ThatContainsQueryParamThrowsIfAnotherMatchForUrlWithQueryStringAlreadyExists()
    {
        // Given
        m_Builder.ForRequest("https://site.net/api/items?filter=value", HttpMethod.Get);
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/other", HttpMethod.Get);

        // When
        var ex = Record.Exception(() => requestBuilder.ThatContainsQueryParam("sort", "name"));

        // Then
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Contains("request matches already exist with QueryString matching", ex.Message);
    }
        
    [Fact]
    public void ThatContainsQueryParamThrowsIfDupeName()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/other", HttpMethod.Get)
                                      .ThatContainsQueryParam("sort", "name");
            
        // When
        var ex = Record.Exception(() => requestBuilder.ThatContainsQueryParam("sort", "name2"));
            
        // Then
        Assert.IsType<ArgumentException>(ex);
        Assert.Contains("already been added", ex.Message);
    }

    #endregion

    #region ThatContainsHeader
        
    [Fact]
    public void ThatContainsHeaderhrowsIfDupeName()
    {
        // Given
        var requestBuilder = m_Builder.ForRequest("https://site.net/api/other", HttpMethod.Get)
                                      .ThatContainsHeader("Authorization", "Bearer x");
            
        // When
        var ex = Record.Exception(() => requestBuilder.ThatContainsHeader("Authorization", "Bearer y"));
            
        // Then
        Assert.IsType<ArgumentException>(ex);
        Assert.Contains("already been added", ex.Message);
    }

    #endregion
        
    #region Build

    [Fact]
    public void BuildDoesNotThrowOnMultipleCalls()
    {
        // Given
        m_Builder.ForRequest("http://test", HttpMethod.Get);
        m_Builder.Build();
            
        // When 
        var ex = Record.Exception(() => m_Builder.Build());
            
        // Then
        Assert.Null(ex);
    }
        
    [Theory]
    [MemberData(nameof(UrlAndMethodData))]
    public void BuildDoesNotThrowOnMultipleCallsWithDuplicateUrlsAndTrackersAttached(string url, HttpMethod method)
    {
        // Given
        m_Builder.ForRequest(url, method)
                 .TrackRequest();
        m_Builder.ForRequest(url, method);
            
        m_Builder.Build();

        // When 
        var ex = Record.Exception(() => m_Builder.Build());
            
        // Then
        Assert.Null(ex);
    }

    #endregion
}