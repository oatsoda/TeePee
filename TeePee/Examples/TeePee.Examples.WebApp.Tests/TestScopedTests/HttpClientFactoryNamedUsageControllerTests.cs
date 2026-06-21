using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeePee.Examples.WebApp.Controllers;

namespace TeePee.Examples.WebApp.Tests.TestScopedTests
{
    public class HttpClientFactoryNamedUsageControllerTests
    {
        private readonly TeePeeBuilder m_TeePeeBuilder = new();
        private const string _NAMED_HTTP_CLIENT = "ThirdPartyApi";


        private readonly IServiceCollection m_AutoInjectionServiceCollection;

        public HttpClientFactoryNamedUsageControllerTests()
        {
            var unitTestConfig = UnitTestConfig.LoadUnitTestConfig();
            m_AutoInjectionServiceCollection = new ServiceCollection()
                // Production Code
                .AddExampleWebAppDependencies(unitTestConfig)
                // Have to register Controller explicitly
                .AddSingleton<HttpClientFactoryNamedUsageController>()
                // Test Overrides
                .AttachToNamedClient(m_TeePeeBuilder, _NAMED_HTTP_CLIENT)
                ;
        }

        #region Manual Injection

        [Fact]
        public async Task ManualInjection_RecommendedPassiveMocking()
        {
            // Given
            m_TeePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Get)
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

            var controller = new HttpClientFactoryNamedUsageController((await m_TeePeeBuilder.Build()).Manual("https://some.api").CreateHttpClientFactory(_NAMED_HTTP_CLIENT));

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
            var requestTracker = m_TeePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Put)
                                                .ThatContainsQueryParam("filter", "other")
                                                .ThatHasBody(new { Caller = "ThisCaller" })
                                                .Responds()
                                                .WithStatus(HttpStatusCode.Created)
                                                .TrackRequest();

            var controller = new HttpClientFactoryNamedUsageController((await m_TeePeeBuilder.Build()).Manual("https://some.api").CreateHttpClientFactory(_NAMED_HTTP_CLIENT));

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
            m_TeePeeBuilder.ForRequest("https://unittest.example.named/path/resource", HttpMethod.Get)
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
                .GetRequiredService<HttpClientFactoryNamedUsageController>();

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
            var requestTracker = m_TeePeeBuilder.ForRequest("https://unittest.example.named/path/resource", HttpMethod.Put)
                                                .ThatContainsQueryParam("filter", "other")
                                                .ThatHasBody(new { Caller = "ThisCaller" })
                                                .Responds()
                                                .WithStatus(HttpStatusCode.Created)
                                                .TrackRequest();

            var controller = m_AutoInjectionServiceCollection.BuildServiceProvider()
                .GetRequiredService<HttpClientFactoryNamedUsageController>();

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