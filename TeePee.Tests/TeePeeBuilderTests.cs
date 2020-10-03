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
                                        .WithBody("test");

            // When
            var ex = Record.Exception(() => requestBuilder.WithBody("test"));

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
            var ex = Record.Exception(() => requestBuilder.ContainingQueryParam("sort", "name"));

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
            var ex = Record.Exception(() => requestBuilder.ContainingQueryParam("sort", "name"));

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
            var ex = Record.Exception(() => requestBuilder.ContainingQueryParam("sort", "name"));

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
                                        .ContainingQueryParam("sort", "name");
            
            // When
            var ex = Record.Exception(() => requestBuilder.ContainingQueryParam("sort", "name2"));
            
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
                                        .ContainingHeader("Authorization", "Bearer x");
            
            // When
            var ex = Record.Exception(() => requestBuilder.ContainingHeader("Authorization", "Bearer y"));
            
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
        public void ForRequestThrowsOnDuplicateUrlAndMethod(string url, HttpMethod method)
        {
            // Given
            var builder = new TeePeeBuilder();
            builder.ForRequest(url, method);

            // When 
            var ex = Record.Exception(() => builder.ForRequest(url, method));

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
            var builder = new TeePeeBuilder();
            builder.ForRequest(firstUrl, firstMethod);

            // When 
            var ex = Record.Exception(() => builder.ForRequest(secondUrl, secondMethod));

            Assert.Null(ex);
        }
    }
}
