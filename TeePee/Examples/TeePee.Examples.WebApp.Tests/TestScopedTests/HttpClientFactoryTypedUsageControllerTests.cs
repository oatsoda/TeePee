using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeePee.Examples.WebApp.Controllers;
using TeePee.UsageExtensions;

namespace TeePee.Examples.WebApp.Tests.TestScopedTests
{
    public class HttpClientFactoryTypedUsageControllerTests
    {
        private readonly TeePeeBuilder m_TeePeeBuilder = new();

        private readonly IServiceCollection m_AutoInjectionServiceCollection;

        public HttpClientFactoryTypedUsageControllerTests()
        {
            var unitTestConfig = UnitTestConfig.LoadUnitTestConfig();
            m_AutoInjectionServiceCollection = new ServiceCollection()
                // Production Code
                .AddExampleWebAppDependencies(unitTestConfig)
                // Have to register Controller explicitly
                .AddSingleton<HttpClientFactoryTypedUsageController>()
                // Test Overrides
                .AttachToTypedClient<ExampleTypedHttpClient>(m_TeePeeBuilder)
                ;
        }

        #region Manual Injection

        [Fact]
        public async Task ManualInjection_RecommendedPassiveMocking()
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

            var controller = new HttpClientFactoryTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilder.Manual("https://some.api").CreateClient()));

            // When
            var result = await controller.FireAndAct();

            // Then
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = Assert.IsType<int>(okResult.Value);
            Assert.Equal(10, resultValue);
        }

        [Fact]
        public async Task ManualInjection_MockAndVerify()
        {
            // Given
            var requestTracker = m_TeePeeBuilder
                .ForRequest("https://some.api/path/resource", HttpMethod.Put)
                .ThatContainsQueryParam("filter", "other")
                .ThatHasBody(new { Caller = "ThisCaller" })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .TrackRequest();

            var controller = new HttpClientFactoryTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilder.Manual("https://some.api").CreateClient()));

            // When
            var result = await controller.FireAndForget();

            // Then
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);

            requestTracker.WasCalled(1);
        }

        #endregion

        #region Auto Injection

        /*
         * Normally, a benefit of using an IoC Container / Auto Injection from within your tests is that you can test without needing to
         * know about the internal implementation details. However, in the case of Typed Http Clients, your tests will need to know
         * the Type of those Typed Http Clients so that it can attach and intercept.
         */

        [Fact]
        public async Task AutoInjection_RecommendedPassiveMocking()
        {
            // Given
            m_TeePeeBuilder
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

            var controller = m_AutoInjectionServiceCollection.BuildServiceProvider()
                .GetRequiredService<HttpClientFactoryTypedUsageController>();

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
                .ForRequest("https://unittest.example.typed/path/resource", HttpMethod.Put)
                .ThatContainsQueryParam("filter", "other")
                .ThatHasBody(new { Caller = "ThisCaller" })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .TrackRequest();

            var controller = m_AutoInjectionServiceCollection.BuildServiceProvider()
                .GetRequiredService<HttpClientFactoryTypedUsageController>();

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