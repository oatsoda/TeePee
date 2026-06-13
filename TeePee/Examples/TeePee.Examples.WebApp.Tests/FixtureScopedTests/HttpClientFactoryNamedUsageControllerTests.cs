using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeePee.Examples.WebApp.Controllers;

namespace TeePee.Examples.WebApp.Tests.FixtureScopedTests
{
    public class HttpClientFactoryNamedUsageControllerTests : IClassFixture<HttpClientFactoryNamedUsageControllerFixture>
    {
        private readonly HttpClientFactoryNamedUsageControllerFixture m_Fixture;

        public HttpClientFactoryNamedUsageControllerTests(HttpClientFactoryNamedUsageControllerFixture fixture)
        {
            m_Fixture = fixture;
            m_Fixture.SetTestScope();
        }

        [Fact]
        public async Task FixturedServiceResolution_RecommendedPassiveMocking()
        {
            // Given
            m_Fixture.TeePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Get)
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
            var requestTracker = m_Fixture.TeePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Put)
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
        private const string _NAMED_HTTP_CLIENT = "ThirdPartyApi";

        public TeePeeBuilder TeePeeBuilder { get; private set; } = new(_NAMED_HTTP_CLIENT);

        public HttpClientFactoryNamedUsageControllerFixture()
        {
        }

        protected override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Need to register IHttpClientFactory here
            // and need to ensure it is configured to respond only for the expected named client.
            // await m_TeePeeBuilder.Build()).Manual("https://some.api").CreateHttpClientFactory()

            services

            // This is actual production code registrations.
                .AddNamedHttpClients(configuration)

            // This overrides the Factory with TeePee's factory for the named client.
            // But there may be times when the SUT doesn't directly use the Factory, and instead a Singleton dependency
            // does, meaining this scoped override will not work.
                .AddScoped<IHttpClientFactory>(_ => TeePeeBuilder.Build().GetAwaiter().GetResult().Manual("https://some.api").CreateHttpClientFactory());

            return services;
        }

        protected override void Reset()
        {
            // HACK for now - because the factory is used by a scoped service. If it were used by a singleton, this would not work.
            TeePeeBuilder = new TeePeeBuilder(_NAMED_HTTP_CLIENT);
        }
    }

    public abstract class BaseFixture<T> : IDisposable where T : class
    {
        private readonly IServiceCollection m_Services;

        private IServiceProvider? m_ServiceProvider;

        private IServiceScope? m_Scope;

        public BaseFixture()
        {
            var config = UnitTestConfig.LoadUnitTestConfig();

            m_Services = new ServiceCollection()
                .AddScoped<T>();

            ConfigureServices(m_Services, config);
        }

        public void SetTestScope()
        {
            if (m_Scope == null)
            {
                // First test execution
                m_ServiceProvider = m_Services.BuildServiceProvider();
            }
            else
            {
                m_Scope.Dispose();
                Reset();
            }

            m_Scope = m_ServiceProvider!.CreateScope();
        }

        protected abstract IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration);
        protected abstract void Reset();

        public T GetSUT() => m_Scope == null
            ? throw new InvalidOperationException("Test scope is not set.")
            : m_Scope.ServiceProvider.GetRequiredService<T>();

        public void Dispose()
        {
            m_Scope?.Dispose();
        }
    }
}
