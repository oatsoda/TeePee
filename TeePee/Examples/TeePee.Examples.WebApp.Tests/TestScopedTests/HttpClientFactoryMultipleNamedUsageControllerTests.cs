using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TeePee.Examples.WebApp.Controllers;
using TeePee.UsageExtensions;

namespace TeePee.Examples.WebApp.Tests.TestScopedTests
{
    public class HttpClientFactoryMultipleNamedUsageControllerTests
    {
        private const string _EXPECTED_NAMED_HTTP_CLIENT_ONE = "OneApi";
        private const string _EXPECTED_NAMED_HTTP_CLIENT_TWO = "TwoApi";
        private readonly TeePeeBuilder m_TeePeeBuilderOne = new();//_NAMED_HTTP_CLIENT_ONE);
        private readonly TeePeeBuilder m_TeePeeBuilderTwo = new();//_NAMED_HTTP_CLIENT_TWO);

        private readonly IServiceCollection m_AutoInjectionServiceCollection;

        public HttpClientFactoryMultipleNamedUsageControllerTests()
        {

            var unitTestConfig = UnitTestConfig.LoadUnitTestConfig();
            m_AutoInjectionServiceCollection = new ServiceCollection()
                // Production Code
                .AddExampleWebAppDependencies(unitTestConfig)
                // Have to register Controller explicitly
                .AddSingleton<HttpClientFactoryMultipleNamedUsageController>()
                // Test Overrides
                .AttachToNamedClient(m_TeePeeBuilderOne, _EXPECTED_NAMED_HTTP_CLIENT_ONE)
                .AttachToNamedClient(m_TeePeeBuilderTwo, _EXPECTED_NAMED_HTTP_CLIENT_TWO)
                ;
        }

        //    #region Manual Injection

        //    [Fact]
        //    public async Task ManualInjection_RecommendedPassiveMocking()
        //    {
        //        // Given
        //        m_TeePeeBuilderOne.ForRequest("https://first.api/path-one/resource-one", HttpMethod.Get)
        //                          .ThatContainsQueryParam("filter", "those")
        //                          .Responds()
        //                          .WithStatus(HttpStatusCode.OK)
        //                          .WithBody(new
        //                          {
        //                              Things = new[]
        //                                                 {
        //                                                     new
        //                                                     {
        //                                                         Value = 10
        //                                                     }
        //                                                 }
        //                          });

        //        m_TeePeeBuilderTwo.ForRequest("https://second.api/path-two/resource-two", HttpMethod.Get)
        //                          .ThatContainsQueryParam("filter", "those")
        //                          .Responds()
        //                          .WithStatus(HttpStatusCode.OK)
        //                          .WithBody(new
        //                          {
        //                              Things = new[]
        //                                                 {
        //                                                     new
        //                                                     {
        //                                                         Value = 30
        //                                                     }
        //                                                 }
        //                          });

        //        var controller = new HttpClientFactoryMultipleNamedUsageController(new[]
        //                                                                               {
        //                                                                                   (await m_TeePeeBuilderOne.Build()).Manual("https://first.api"),
        //                                                                                   (await m_TeePeeBuilderTwo.Build()).Manual("https://second.api")
        //                                                                               }
        //                                                                              .ToHttpClientFactory());

        //        // When
        //        var result = await controller.FireAndAct();

        //        // Then
        //        Assert.NotNull(result);
        //        var okResult = Assert.IsType<OkObjectResult>(result);
        //        var resultValue = Assert.IsType<int>(okResult.Value);
        //        Assert.Equal(40, resultValue);
        //    }

        //    [Fact]
        //    public async Task ManualInjection_MockAndVerify()
        //    {
        //        // Given
        //        var requestTrackerOne = m_TeePeeBuilderOne.ForRequest("https://first.api/path-one/resource-one", HttpMethod.Put)
        //                                                  .ThatContainsQueryParam("filter", "other")
        //                                                  .ThatHasBody(new { Caller = "ThisCaller" })
        //                                                  .Responds()
        //                                                  .WithStatus(HttpStatusCode.Created)
        //                                                  .TrackRequest();

        //        var requestTrackerTwo = m_TeePeeBuilderTwo.ForRequest("https://second.api/path-two/resource-two", HttpMethod.Put)
        //                                                  .ThatContainsQueryParam("filter", "other")
        //                                                  .ThatHasBody(new { Caller = "ThisCaller" })
        //                                                  .Responds()
        //                                                  .WithStatus(HttpStatusCode.Created)
        //                                                  .TrackRequest();

        //        var controller = new HttpClientFactoryMultipleNamedUsageController(new[]
        //                                                                               {
        //                                                                                   (await m_TeePeeBuilderOne.Build()).Manual("https://first.api"),
        //                                                                                   (await m_TeePeeBuilderTwo.Build()).Manual("https://second.api")
        //                                                                               }
        //                                                                              .ToHttpClientFactory());

        //        // When
        //        var result = await controller.FireAndForget();

        //        // Then
        //        Assert.NotNull(result);
        //        Assert.IsType<OkResult>(result);

        //        requestTrackerOne.WasCalled(1);
        //        requestTrackerTwo.WasCalled(1);
        //    }

        //    #endregion

        #region Auto Injection

        [Fact]
        public async Task AutoInjection_RecommendedPassiveMocking()
        {
            // Given
            m_TeePeeBuilderOne
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

            m_TeePeeBuilderTwo
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

            var controller = m_AutoInjectionServiceCollection.BuildServiceProvider()
                .GetRequiredService<HttpClientFactoryMultipleNamedUsageController>();

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
            var requestTrackerOne = m_TeePeeBuilderOne
                .ForRequest("https://unittest.multipleone.named/path-one/resource-one", HttpMethod.Put)
                .ThatContainsQueryParam("filter", "other")
                .ThatHasBody(new { Caller = "ThisCaller" })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .TrackRequest();

            var requestTrackerTwo = m_TeePeeBuilderTwo
                .ForRequest("https://unittest.multipletwo.named/path-two/resource-two", HttpMethod.Put)
                .ThatContainsQueryParam("filter", "other")
                .ThatHasBody(new { Caller = "ThisCaller" })
                .Responds()
                .WithStatus(HttpStatusCode.Created)
                .TrackRequest();

            var controller = m_AutoInjectionServiceCollection.BuildServiceProvider()
                .GetRequiredService<HttpClientFactoryMultipleNamedUsageController>();

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
