using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeePee.Examples.WebApp.Controllers;

namespace TeePee.Examples.WebApp.Tests.FixtureScopedTests
{
    public class FixturedHttpClientFactoryMultipleNamedUsageControllerTests : IClassFixture<HttpClientFactoryMultipleNamedUsageControllerFixture>
    {
        private readonly HttpClientFactoryMultipleNamedUsageControllerFixture m_Fixture;

        public FixturedHttpClientFactoryMultipleNamedUsageControllerTests(HttpClientFactoryMultipleNamedUsageControllerFixture fixture)
        {
            m_Fixture = fixture;
            m_Fixture.SetTestScope();
        }

        [Fact]
        public async Task FixturedServiceResolution_RecommendedPassiveMocking()
        {
            // Given
            m_Fixture.TeePeeBuilderOne
                .ForRequest("https://unittest.multipleone.named/path-one/resource-one", HttpMethod.Get)
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
                .ForRequest("https://unittest.multipletwo.named/path-two/resource-two", HttpMethod.Get)
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
        public async Task FixturedServiceResolution_MockAndVerify()
        {
            // Given
            var requestTrackerOne = m_Fixture.TeePeeBuilderOne
                .ForRequest("https://unittest.multipleone.named/path-one/resource-one", HttpMethod.Put)
                .ThatContainsQueryParam("filter", "other")
                .ThatHasBody(new { Caller = "ThisCaller" })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .TrackRequest();

            var requestTrackerTwo = m_Fixture.TeePeeBuilderTwo
                .ForRequest("https://unittest.multipletwo.named/path-two/resource-two", HttpMethod.Put)
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

    public class HttpClientFactoryMultipleNamedUsageControllerFixture : BaseFixture<HttpClientFactoryMultipleNamedUsageController>
    {
        private const string _EXPECTED_NAMED_HTTP_CLIENT_ONE = "OneApi";
        private const string _EXPECTED_NAMED_HTTP_CLIENT_TWO = "TwoApi";

        public TeePeeBuilder TeePeeBuilderOne { get; } = new();
        public TeePeeBuilder TeePeeBuilderTwo { get; } = new();

        protected override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            return services
                // This is actual production code registrations.
                .AddNamedHttpClients(configuration)
                // Test code
                .AttachToNamedClient(TeePeeBuilderOne, _EXPECTED_NAMED_HTTP_CLIENT_ONE)
                .AttachToNamedClient(TeePeeBuilderTwo, _EXPECTED_NAMED_HTTP_CLIENT_TWO);
        }

        protected override void Reset()
        {
            TeePeeBuilderOne.Reset();
            TeePeeBuilderTwo.Reset();
        }
    }
}
