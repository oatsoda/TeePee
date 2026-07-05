using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeePee.Examples.WebApp.Controllers;

namespace TeePee.Examples.WebApp.Tests.FixtureScopedTests
{
    public class FixturedHttpClientFactoryMultipleTypedUsageControllerTests : IClassFixture<HttpClientFactoryMultipleTypedUsageControllerFixture>
    {
        private readonly HttpClientFactoryMultipleTypedUsageControllerFixture m_Fixture;

        public FixturedHttpClientFactoryMultipleTypedUsageControllerTests(HttpClientFactoryMultipleTypedUsageControllerFixture fixture)
        {
            m_Fixture = fixture;
            m_Fixture.SetTestScope();
        }

        [Fact]
        public async Task AutoInjection_RecommendedPassiveMocking()
        {
            // Given
            m_Fixture.TeePeeBuilderOne
                .ForRequest("https://unittest.example.typed/path/resource", HttpMethod.Get)
                .ThatContainsQueryParam("filter", "those")
                .Responds()
                .WithStatus(HttpStatusCode.OK)
                .WithBody(new
                {
                    Things = new[]
                    {
                        new
                        {
                            Value = 10
                        }
                    }
                });

            m_Fixture.TeePeeBuilderTwo
                .ForRequest("https://unittest.anotherexample.typed/path/other-resource", HttpMethod.Get)
                .ThatContainsQueryParam("filter", "those")
                .Responds()
                .WithStatus(HttpStatusCode.OK)
                .WithBody(new
                {
                    Things = new[]
                    {
                        new
                        {
                            Value = 30
                        }
                    }
                });

            var controller = m_Fixture.GetSUT();

            // When
            var result = await controller.FireAndAct();

            // Then
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = Assert.IsType<int>(okResult.Value);
            Assert.Equal(40, resultValue);
        }

        [Fact]
        public async Task AutoInjection_MockAndVerify()
        {
            // Given
            var requestTrackerOne = m_Fixture.TeePeeBuilderOne
                .ForRequest("https://unittest.example.typed/path/resource", HttpMethod.Put)
                .ThatContainsQueryParam("filter", "other")
                .ThatHasBody(new { Caller = "ThisCaller" })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .TrackRequest();

            var requestTrackerTwo = m_Fixture.TeePeeBuilderTwo
                .ForRequest("https://unittest.anotherexample.typed/path/other-resource", HttpMethod.Put)
                .ThatContainsQueryParam("filter", "other")
                .ThatHasBody(new { Caller = "ThisCaller" })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .TrackRequest();

            var controller = m_Fixture.GetSUT();

            // When
            var result = await controller.FireAndForget();

            // Then
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);

            requestTrackerOne.WasCalled(1);
            requestTrackerTwo.WasCalled(1);
        }
    }

    public class HttpClientFactoryMultipleTypedUsageControllerFixture : BaseFixture<HttpClientFactoryMultipleTypedUsageController>
    {
        public TeePeeBuilder TeePeeBuilderOne { get; } = new();
        public TeePeeBuilder TeePeeBuilderTwo { get; } = new();

        protected override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            /*
             * Normally, a benefit of using an IoC Container / Auto Injection from within your tests is that you can test without needing to
             * know about the internal implementation details. However, in the case of Typed Http Clients, your tests will need to know
             * the Type of those Typed Http Clients so that it can attach and intercept.
             */

            return services
                // This is actual production code registrations.
                .AddTypedHttpClients(configuration)
                // Test code
                .AttachToTypedClient<ExampleTypedHttpClient>(TeePeeBuilderOne)
                .AttachToTypedClient<AnotherExampleTypedHttpClient>(TeePeeBuilderTwo);
        }

        protected override void Reset()
        {
            TeePeeBuilderOne.Reset();
            TeePeeBuilderTwo.Reset();
        }
    }
}
