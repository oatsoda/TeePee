using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TeePee.DependencyInjection;
using TeePee.Examples.WebApp.Controllers;
using Xunit;

namespace TeePee.Examples.WebApp.Tests
{
    public class HttpClientFactoryTypedUsageControllerTests
    {
        private readonly TeePeeBuilder m_TeePeeBuilder = new();

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

            var controller = new HttpClientFactoryTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilder.Build().Manual("https://some.api").CreateClient()));

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

            var controller = new HttpClientFactoryTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilder.Build().Manual("https://some.api").CreateClient()));

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
            m_TeePeeBuilder.ForRequest("https://unittest.example.typed/path/resource", HttpMethod.Get)
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

            var controller = Resolve.WithTypedClient<HttpClientFactoryTypedUsageController, ExampleTypedHttpClient>(m_TeePeeBuilder,
                                                                                                                   sc =>
                                                                                                                   {
                                                                                                                       /* Example of using prod Setup code */
                                                                                                                       var configuration = UnitTestConfig.LoadUnitTestConfig();

                                                                                                                       // Call your production code, which sets up the Typed Client, here
                                                                                                                       sc.AddTypedHttpClients(configuration);
                                                                                                                   });

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
            var requestTracker = m_TeePeeBuilder.ForRequest("https://unittest.example.typed/path/resource", HttpMethod.Put)
                                                .ThatContainsQueryParam("filter", "other")
                                                .ThatHasBody(new { Caller = "ThisCaller" })
                                                .Responds()
                                                .WithStatus(HttpStatusCode.Created)
                                                .TrackRequest();

            var controller = Resolve.WithTypedClient<HttpClientFactoryTypedUsageController, ExampleTypedHttpClient>(m_TeePeeBuilder,
                                                                                                                    sc =>
                                                                                                                    {
                                                                                                                        var configuration = UnitTestConfig.LoadUnitTestConfig();

                                                                                                                        // Call your production code, which sets up the Typed Client, here
                                                                                                                        sc.AddTypedHttpClients(configuration);
                                                                                                                    });

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