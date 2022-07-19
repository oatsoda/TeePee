using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TeePee.DependencyInjection;
using TeePee.Examples.WebApp.Controllers;
using Xunit;

namespace TeePee.Examples.WebApp.Tests
{
    public class HttpClientFactoryMultipleNamedUsageControllerTests
    {
        private const string _NAMED_HTTP_CLIENT_ONE = "OneApi";
        private const string _NAMED_HTTP_CLIENT_TWO = "TwoApi";
        private readonly TeePeeBuilder m_TeePeeBuilderOne = new TeePeeBuilder(_NAMED_HTTP_CLIENT_ONE);
        private readonly TeePeeBuilder m_TeePeeBuilderTwo = new TeePeeBuilder(_NAMED_HTTP_CLIENT_TWO);

        #region Manual Injection

        [Fact]
        public async Task ManualInjection_RecommendedPassiveMocking()
        {
            // Given
            m_TeePeeBuilderOne.ForRequest("https://first.api/pathone/resourceone", HttpMethod.Get)
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

            m_TeePeeBuilderTwo.ForRequest("https://second.api/pathtwo/resourcetwo", HttpMethod.Get)
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

            var controller = new HttpClientFactoryMultipleNamedUsageController(new[] { m_TeePeeBuilderOne.Build().Manual("https://first.api"), m_TeePeeBuilderTwo.Build().Manual("https://second.api") }
                                                                                  .ToHttpClientFactory());

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
            var requestTrackerOne = m_TeePeeBuilderOne.ForRequest("https://first.api/pathone/resourceone", HttpMethod.Put)
                                                      .ThatContainsQueryParam("filter", "other")
                                                      .ThatHasBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var requestTrackerTwo = m_TeePeeBuilderTwo.ForRequest("https://second.api/pathtwo/resourcetwo", HttpMethod.Put)
                                                      .ThatContainsQueryParam("filter", "other")
                                                      .ThatHasBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();
            
            var controller = new HttpClientFactoryMultipleNamedUsageController(new[] { m_TeePeeBuilderOne.Build().Manual("https://first.api"), m_TeePeeBuilderTwo.Build().Manual("https://second.api") }
                                                                                  .ToHttpClientFactory());

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

        [Fact]
        public async Task AutoInjection_RecommendedPassiveMocking()
        {
            // Given
            m_TeePeeBuilderOne.ForRequest("https://unittest.multipleone.named/pathone/resourceone", HttpMethod.Get)
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

            m_TeePeeBuilderTwo.ForRequest("https://unittest.multipletwo.named/pathtwo/resourcetwo", HttpMethod.Get)
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

            var controller = Resolve.WithNamedClients<HttpClientFactoryMultipleNamedUsageController>(sc =>
                                                                                                     {
                                                                                                         var configuration = UnitTestConfig.LoadUnitTestConfig();

                                                                                                         // Call your production code, which sets up the Typed Client, here
                                                                                                         sc.AddNamedHttpClients(configuration);
                                                                                                     },
                                                                                                     m_TeePeeBuilderOne,
                                                                                                     m_TeePeeBuilderTwo);

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
            var requestTrackerOne = m_TeePeeBuilderOne.ForRequest("https://unittest.multipleone.named/pathone/resourceone", HttpMethod.Put)
                                                      .ThatContainsQueryParam("filter", "other")
                                                      .ThatHasBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var requestTrackerTwo = m_TeePeeBuilderTwo.ForRequest("https://unittest.multipletwo.named/pathtwo/resourcetwo", HttpMethod.Put)
                                                      .ThatContainsQueryParam("filter", "other")
                                                      .ThatHasBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var controller = Resolve.WithNamedClients<HttpClientFactoryMultipleNamedUsageController>(sc =>
                                                                                                     {
                                                                                                         var configuration = UnitTestConfig.LoadUnitTestConfig();

                                                                                                         // Call your production code, which sets up the Typed Client, here
                                                                                                         sc.AddNamedHttpClients(configuration);
                                                                                                     },
                                                                                                     m_TeePeeBuilderOne,
                                                                                                     m_TeePeeBuilderTwo);

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