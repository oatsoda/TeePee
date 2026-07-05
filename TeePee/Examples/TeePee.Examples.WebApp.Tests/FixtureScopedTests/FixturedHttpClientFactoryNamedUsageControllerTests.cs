using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeePee.Examples.WebApp.Controllers;

namespace TeePee.Examples.WebApp.Tests.FixtureScopedTests
{
    public class FixturedHttpClientFactoryNamedUsageControllerTests : IClassFixture<HttpClientFactoryNamedUsageControllerFixture>
    {
        private readonly HttpClientFactoryNamedUsageControllerFixture m_Fixture;

        public FixturedHttpClientFactoryNamedUsageControllerTests(HttpClientFactoryNamedUsageControllerFixture fixture)
        {
            m_Fixture = fixture;
            m_Fixture.SetTestScope();
        }

        [Fact]
        public async Task FixturedServiceResolution_RecommendedPassiveMocking()
        {
            // Given
            m_Fixture.TeePeeBuilder
                .ForRequest("https://unittest.example.named/path/resource", HttpMethod.Get)
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

            var controller = m_Fixture.GetSUT();

            // When
            var result = await controller.FireAndAct();

            // Then
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = Assert.IsType<int>(okResult.Value);
            Assert.Equal(10, resultValue);
        }

        [Fact]
        public async Task FixturedServiceResolution_MockAndVerify()
        {
            // Given
            var requestTracker = m_Fixture.TeePeeBuilder
                .ForRequest("https://unittest.example.named/path/resource", HttpMethod.Put)
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

            requestTracker.WasCalled(1);
        }
    }

    public class HttpClientFactoryNamedUsageControllerFixture : BaseFixture<HttpClientFactoryNamedUsageController>
    {
        private const string _EXPECTED_NAMED_HTTP_CLIENT = "ThirdPartyApi";

        public TeePeeBuilder TeePeeBuilder { get; } = new();

        protected override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            return services
                // This is actual production code registrations.
                .AddNamedHttpClients(configuration)
                // Test code
                .AttachToNamedClient(TeePeeBuilder, _EXPECTED_NAMED_HTTP_CLIENT);
        }

        protected override void Reset()
        {
            TeePeeBuilder.Reset();
        }
    }
}
