using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TeePee.DependencyInjection;
using TeePee.Examples.WebApp.Controllers;
using Xunit;

namespace TeePee.Examples.WebApp.Tests
{
    public class HttpClientFactoryMultipleTypedUsageControllerTests
    {
        private readonly TeePeeBuilder<ExampleTypedHttpClient> m_TeePeeBuilderOne = new TeePeeBuilder<ExampleTypedHttpClient>();
        private readonly TeePeeBuilder<AnotherExampleTypedHttpClient> m_TeePeeBuilderTwo = new TeePeeBuilder<AnotherExampleTypedHttpClient>();

        #region Manual Injection

        [Fact]
        public async Task ManualInjection_RecommendedPassiveMocking()
        {
            // Given
            m_TeePeeBuilderOne.ForRequest("https://some.api/path/resource", HttpMethod.Get)
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

            m_TeePeeBuilderTwo.ForRequest("https://other.api/path/otherresource", HttpMethod.Get)
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


            var controller = new HttpClientFactoryMultipleTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilderOne.Build().Manual("https://some.api").CreateClient()), 
                                                                               new AnotherExampleTypedHttpClient(m_TeePeeBuilderTwo.Build().Manual("https://other.api").CreateClient()));

            // When
            var result = await controller.FireAndAct();

            // Then
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultValue = Assert.IsType<int>(okResult.Value);
            Assert.Equal(40, resultValue);
        }

        [Fact]
        public async Task ManualInjection_MockAndVerify()
        {
            // Given
            var requestTrackerOne = m_TeePeeBuilderOne.ForRequest("https://some.api/path/resource", HttpMethod.Put)
                                                      .ThatContainsQueryParam("filter", "other")
                                                      .ThatHasJsonBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var requestTrackerTwo = m_TeePeeBuilderTwo.ForRequest("https://other.api/path/otherresource", HttpMethod.Put)
                                                      .ThatContainsQueryParam("filter", "other")
                                                      .ThatHasJsonBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();
            
            var controller = new HttpClientFactoryMultipleTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilderOne.Build().Manual("https://some.api").CreateClient()), 
                                                                               new AnotherExampleTypedHttpClient(m_TeePeeBuilderTwo.Build().Manual("https://other.api").CreateClient()));

            // When
            var result = await controller.FireAndForget();

            // Then
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);

            requestTrackerOne.WasCalled(1);
            requestTrackerTwo.WasCalled(1);
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
            m_TeePeeBuilderOne.ForRequest("https://unittest.example.typed/path/resource", HttpMethod.Get)
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

            m_TeePeeBuilderTwo.ForRequest("https://unittest.anotherexample.typed/path/otherresource", HttpMethod.Get)
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

            var controller = Resolve.WithTypedClients<HttpClientFactoryMultipleTypedUsageController, ExampleTypedHttpClient, AnotherExampleTypedHttpClient>(m_TeePeeBuilderOne, m_TeePeeBuilderTwo, sc =>
                                                                                                                                                                                                    {
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
            Assert.Equal(40, resultValue);
        }

        [Fact]
        public async Task AutoInjection_MockAndVerify()
        {
            // Given
            var requestTrackerOne = m_TeePeeBuilderOne.ForRequest("https://unittest.example.typed/path/resource", HttpMethod.Put)
                                                      .ThatContainsQueryParam("filter", "other")
                                                      .ThatHasJsonBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var requestTrackerTwo = m_TeePeeBuilderTwo.ForRequest("https://unittest.anotherexample.typed/path/otherresource", HttpMethod.Put)
                                                      .ThatContainsQueryParam("filter", "other")
                                                      .ThatHasJsonBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var controller = Resolve.WithTypedClients<HttpClientFactoryMultipleTypedUsageController, ExampleTypedHttpClient, AnotherExampleTypedHttpClient>(m_TeePeeBuilderOne, m_TeePeeBuilderTwo, sc =>
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

            requestTrackerOne.WasCalled(1);
            requestTrackerTwo.WasCalled(1);
        }

        #endregion
    }
}