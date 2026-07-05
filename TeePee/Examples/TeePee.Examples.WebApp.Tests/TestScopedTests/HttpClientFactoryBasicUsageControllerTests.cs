using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeePee.Examples.WebApp.Controllers;
using TeePee.UsageExtensions;

namespace TeePee.Examples.WebApp.Tests.TestScopedTests
{
    // Basic usage of HttpClientFactory (i.e. non-named or type clients is only really meant as a refactoring step)
    public class HttpClientFactoryBasicUsageControllerTests
    {
        private readonly TeePeeBuilder m_TeePeeBuilder = new();

        private readonly IServiceCollection m_AutoInjectionServiceCollection;

        public HttpClientFactoryBasicUsageControllerTests()
        {
            var unitTestConfig = UnitTestConfig.LoadUnitTestConfig();
            m_AutoInjectionServiceCollection = new ServiceCollection()
                // Production Code
                .AddExampleWebAppDependencies(unitTestConfig)
                // Have to register Controller explicitly
                .AddSingleton<HttpClientFactoryBasicUsageController>()
                // Test Overrides
                .AttachToDefaultClient(m_TeePeeBuilder)
                ;
        }

        #region Manual Injection

        [Fact]
        public async Task RecommendedPassiveMocking()
        {
            // Given
            m_TeePeeBuilder
                .ForRequest("https://some.api/path/resource", HttpMethod.Get)
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

            var controller = new HttpClientFactoryBasicUsageController(m_TeePeeBuilder.Manual().CreateHttpClientFactory(""));

            // When
            var result = await controller.FireAndAct();

            // Then
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = Assert.IsType<int>(okResult.Value);
            Assert.Equal(10, resultValue);
        }

        [Fact]
        public async Task MockAndVerify()
        {
            // Given
            var requestTracker = m_TeePeeBuilder
                .ForRequest("https://some.api/path/resource", HttpMethod.Put)
                .ThatContainsQueryParam("filter", "other")
                .ThatHasBody(new { Caller = "ThisCaller" })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .TrackRequest();

            var controller = new HttpClientFactoryBasicUsageController(m_TeePeeBuilder.Manual().CreateHttpClientFactory(""));

            // When
            var result = await controller.FireAndForget();

            // Then
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);

            requestTracker.WasCalled(1);
        }

        #endregion

        #region Auto Injection 

        [Fact]
        public async Task AutoInjection_RecommendedPassiveMocking()
        {
            // Given
            m_TeePeeBuilder
                .ForRequest("https://some.api/path/resource", HttpMethod.Get)
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

            var controller = m_AutoInjectionServiceCollection.BuildServiceProvider()
                .GetRequiredService<HttpClientFactoryBasicUsageController>();

            // When
            var result = await controller.FireAndAct();

            // Then
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = Assert.IsType<int>(okResult.Value);
            Assert.Equal(10, resultValue);
        }

        [Fact]
        public async Task AutoInjection_MockAndVerify()
        {
            // Given
            var requestTracker = m_TeePeeBuilder
                .ForRequest("https://some.api/path/resource", HttpMethod.Put)
                .ThatContainsQueryParam("filter", "other")
                .ThatHasBody(new { Caller = "ThisCaller" })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .TrackRequest();

            var controller = m_AutoInjectionServiceCollection.BuildServiceProvider()
                .GetRequiredService<HttpClientFactoryBasicUsageController>();

            // When
            var result = await controller.FireAndForget();

            // Then
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);

            requestTracker.WasCalled(1);
        }

        #endregion
    }
}
