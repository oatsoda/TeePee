using System;
using System.Collections.Generic;
using System.Net.Http;
using Xunit;

namespace TeePee.Tests
{
    public class TeePeeBuilderTests
    {
        [Theory]
        [InlineData("/api/items")]
        [InlineData("www.site.com/api/items")]
        [InlineData("")]
        [InlineData(" ")]
        public void ForRequestThrowsIfNotAbsolute(string relativeUrl)
        {
            // Given
            var builder = new TeePeeBuilder();

            // When
            var ex = Record.Exception(() => builder.ForRequest(relativeUrl, HttpMethod.Get));

            // Then
            Assert.IsType<ArgumentException>(ex);
            Assert.Contains("must be an absolute URI", ex.Message);
        }
        
        [Theory]
        [InlineData("http://www.site.com/api/items")]
        [InlineData("https://site.com")]
        public void WithUrlDoesNotThrowIfAbsolute(string absoluteUrl)
        {
            // Given
            var builder = new TeePeeBuilder();

            // When
            var ex = Record.Exception(() => builder.ForRequest(absoluteUrl, HttpMethod.Get));

            // Then
            Assert.Null(ex);
        }
        
        [Fact]
        public void WithBodyThrowsIfCalledMoreThanOnce()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                        .ThatHasBody("test");

            // When
            var ex = Record.Exception(() => requestBuilder.ThatHasBody("test"));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("matching Body has already been added", ex.Message);
        }

        [Fact]
        public void WithBodyThrowsIfWithHttpContentBodyAlreadyCalled()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                        .ThatHasHttpContentBody(new StringContent(""));

            // When
            var ex = Record.Exception(() => requestBuilder.ThatHasBody("test"));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("matching Body has already been added", ex.Message);
        }

        [Fact]
        public void WithBodyThrowsIfWithBodyContainingAlreadyCalled()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                        .ThatHasBodyContaining<object>(_ => true);

            // When
            var ex = Record.Exception(() => requestBuilder.ThatHasBody("test"));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("matching Body has already been added", ex.Message);
        }
        
        [Fact]
        public void WithHttpContentBodyThrowsIfCalledMoreThanOnce()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                        .ThatHasHttpContentBody(new StringContent(""));

            // When
            var ex = Record.Exception(() => requestBuilder.ThatHasHttpContentBody(new StringContent("")));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("matching Body has already been added", ex.Message);
        }

        [Fact]
        public void WithHttpContentBodyThrowsIfWithBodyAlreadyCalled()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                        .ThatHasBody("test");

            // When
            var ex = Record.Exception(() => requestBuilder.ThatHasHttpContentBody(new StringContent("")));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("matching Body has already been added", ex.Message);
        }

        [Fact]
        public void WithHttpContentBodyThrowsIfWithBodyContainingAlreadyCalled()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                        .ThatHasBodyContaining<object>(_ => true);

            // When
            var ex = Record.Exception(() => requestBuilder.ThatHasHttpContentBody(new StringContent("")));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("matching Body has already been added", ex.Message);
        }
        
        [Fact]
        public void WithBodyContainingThrowsIfCalledMoreThanOnce()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                        .ThatHasBodyContaining<object>(_ => true);

            // When
            var ex = Record.Exception(() => requestBuilder.ThatHasBodyContaining<object>(_ => true));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("matching Body has already been added", ex.Message);
        }

        [Fact]
        public void WithBodyContainingThrowsIfWithBodyAlreadyCalled()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                        .ThatHasBody("test");

            // When
            var ex = Record.Exception(() => requestBuilder.ThatHasBodyContaining<object>(_ => true));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("matching Body has already been added", ex.Message);
        }

        [Fact]
        public void WithBodyContainingThrowsIfWithHttpContentBodyAlreadyCalled()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items", HttpMethod.Get)
                                        .ThatHasHttpContentBody(new StringContent(""));

            // When
            var ex = Record.Exception(() => requestBuilder.ThatHasBodyContaining<object>(_ => true));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("matching Body has already been added", ex.Message);
        }

        [Fact]
        public void ContainingQueryParamThrowsIfUrlHasQueryString()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/items?filter=value", HttpMethod.Get);

            // When
            var ex = Record.Exception(() => requestBuilder.ThatContainsQueryParam("sort", "name"));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("Url has already been configured to match with a QueryString", ex.Message);
        }
        
        [Fact]
        public void WithUrlThrowsIfAnotherMatchContainingQueryParamAlreadyExists()
        {
            // Given
            var builder = new TeePeeBuilder();
            builder.ForRequest("https://site.net/api/items?filter=value", HttpMethod.Get);
            var requestBuilder = builder.ForRequest("https://site.net/api/other", HttpMethod.Get);

            // When
            var ex = Record.Exception(() => requestBuilder.ThatContainsQueryParam("sort", "name"));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("request matches already exist with QueryString matching", ex.Message);
        }
        
        [Fact]
        public void ContainingQueryParamThrowsIfAnotherMatchForUrlWithQueryStringAlreadyExists()
        {
            // Given
            var builder = new TeePeeBuilder();
            builder.ForRequest("https://site.net/api/items?filter=value", HttpMethod.Get);
            var requestBuilder = builder.ForRequest("https://site.net/api/other", HttpMethod.Get);

            // When
            var ex = Record.Exception(() => requestBuilder.ThatContainsQueryParam("sort", "name"));

            // Then
            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("request matches already exist with QueryString matching", ex.Message);
        }
        
        [Fact]
        public void ContainingQueryParamThrowsIfDupeName()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/other", HttpMethod.Get)
                                        .ThatContainsQueryParam("sort", "name");
            
            // When
            var ex = Record.Exception(() => requestBuilder.ThatContainsQueryParam("sort", "name2"));
            
            // Then
            Assert.IsType<ArgumentException>(ex);
            Assert.Contains("already been added", ex.Message);
        }
        
        [Fact]
        public void ContainingHeaderThrowsIfDupeName()
        {
            // Given
            var builder = new TeePeeBuilder();
            var requestBuilder = builder.ForRequest("https://site.net/api/other", HttpMethod.Get)
                                        .ThatContainsHeader("Authorization", "Bearer x");
            
            // When
            var ex = Record.Exception(() => requestBuilder.ThatContainsHeader("Authorization", "Bearer y"));
            
            // Then
            Assert.IsType<ArgumentException>(ex);
            Assert.Contains("already been added", ex.Message);
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
            var builder = new TeePeeBuilder();
            builder.ForRequest(url, method);

            // When 
            var ex = Record.Exception(() => builder.ForRequest(url, method));
            
            // Then
            Assert.Null(ex);
        }

        [Theory]
        [MemberData(nameof(UrlAndMethodData))]
        public void ForRequestThrowsOnDuplicateUrlAndMethodIfUniqueUrlsRequired(string url, HttpMethod method)
        {
            // Given
            var builder = new TeePeeBuilder(opt => opt.BuilderMode = TeePeeBuilderMode.RequireUniqueUrlRules);
            builder.ForRequest(url, method);

            // When 
            var ex = Record.Exception(() => builder.ForRequest(url, method));
            
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
            var builder = new TeePeeBuilder(opt => opt.BuilderMode = TeePeeBuilderMode.RequireUniqueUrlRules);
            builder.ForRequest(firstUrl, firstMethod);

            // When 
            var ex = Record.Exception(() => builder.ForRequest(secondUrl, secondMethod));

            Assert.Null(ex);
        }

        [Fact]
        public void BuildDoesNotThrowOnMultipleCalls()
        {
            // Given
            var builder = new TeePeeBuilder();
            builder.ForRequest("http://test", HttpMethod.Get);
            builder.Build();
            
            // When 
            var ex = Record.Exception(() => builder.Build());

            Assert.Null(ex);
        }
        
        [Theory]
        [MemberData(nameof(UrlAndMethodData))]
        public void BuildDoesNotThrowOnMultipleCallsWithDuplicateUrlsAndTrackersAttached(string url, HttpMethod method)
        {
            // Given
            var builder = new TeePeeBuilder();
            builder.ForRequest(url, method)
                   .TrackRequest();
            builder.ForRequest(url, method);
            
            builder.Build();

            // When 
            var ex = Record.Exception(() => builder.Build());
            
            // Then
            Assert.Null(ex);
        }
    }
}
