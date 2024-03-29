using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TeePee.DependencyInjection;
using TeePee.Examples.WebApp.Controllers;
using Xunit;

namespace TeePee.Examples.WebApp.Tests
{
    public class HttpClientFactoryNamedUsageControllerTests
    {
        private readonly TeePeeBuilder m_TeePeeBuilder = new(_NAMED_HTTP_CLIENT);
        private const string _NAMED_HTTP_CLIENT = "ThirdPartyApi";

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

            var controller = new HttpClientFactoryNamedUsageController((await m_TeePeeBuilder.Build()).Manual("https://some.api").CreateHttpClientFactory());

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

            var controller = new HttpClientFactoryNamedUsageController((await m_TeePeeBuilder.Build()).Manual("https://some.api").CreateHttpClientFactory());

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

            var controller = await Resolve.WithNamedClients<HttpClientFactoryNamedUsageController>(sc =>
                                                                                             {
                                                                                                 var configuration = UnitTestConfig.LoadUnitTestConfig();

                                                                                                 // Call your production code, which sets up the Typed Client, here
                                                                                                 sc.AddNamedHttpClients(configuration);
                                                                                             },
                                                                                             m_TeePeeBuilder);

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

            var controller = await Resolve.WithNamedClients<HttpClientFactoryNamedUsageController>(sc =>
                                                                                             {
                                                                                                 var configuration = UnitTestConfig.LoadUnitTestConfig();

                                                                                                 // Call your production code, which sets up the Typed Client, here
                                                                                                 sc.AddNamedHttpClients(configuration);
                                                                                             },
                                                                                             m_TeePeeBuilder);

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