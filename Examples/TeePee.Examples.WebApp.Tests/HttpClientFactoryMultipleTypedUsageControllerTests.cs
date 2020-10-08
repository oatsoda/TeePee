using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
                              .ContainingQueryParam("filter", "those")
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
                              .ContainingQueryParam("filter", "those")
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


            var controller = new HttpClientFactoryMultipleTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilderOne.Build().CreateClient()), new AnotherExampleTypedHttpClient(m_TeePeeBuilderTwo.Build().CreateClient()));

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
                                                      .ContainingQueryParam("filter", "other")
                                                      .WithBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var requestTrackerTwo = m_TeePeeBuilderTwo.ForRequest("https://other.api/path/otherresource", HttpMethod.Put)
                                                      .ContainingQueryParam("filter", "other")
                                                      .WithBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var controller = new HttpClientFactoryMultipleTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilderOne.Build().CreateClient()), new AnotherExampleTypedHttpClient(m_TeePeeBuilderTwo.Build().CreateClient()));

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
            m_TeePeeBuilderOne.ForRequest("https://some.api/path/resource", HttpMethod.Get)
                              .ContainingQueryParam("filter", "those")
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
                              .ContainingQueryParam("filter", "those")
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

            var controller = ResolveWithTypedClient<HttpClientFactoryMultipleTypedUsageController, ExampleTypedHttpClient, AnotherExampleTypedHttpClient>(m_TeePeeBuilderOne, m_TeePeeBuilderTwo, sc =>
                                                                                                                                                                                          {
                                                                                                                                                                                              /* Example of using prod Setup code */
                                                                                                                                                                                              var configuration = new ConfigurationBuilder()
                                                                                                                                                                                                                 .AddJsonFile("appsettings.unittests.json")
                                                                                                                                                                                                                 .Build();

                                                                                                                                                                                              // Call your production code, which sets up the Typed Client, here
                                                                                                                                                                                              sc.AddDependencies(configuration);
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
            var requestTrackerOne = m_TeePeeBuilderOne.ForRequest("https://some.api/path/resource", HttpMethod.Put)
                                                      .ContainingQueryParam("filter", "other")
                                                      .WithBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var requestTrackerTwo = m_TeePeeBuilderTwo.ForRequest("https://other.api/path/otherresource", HttpMethod.Put)
                                                      .ContainingQueryParam("filter", "other")
                                                      .WithBody(new { Caller = "ThisCaller" })
                                                      .Responds()
                                                      .WithStatus(HttpStatusCode.Created)
                                                      .TrackRequest();

            var controller = ResolveWithTypedClient<HttpClientFactoryMultipleTypedUsageController, ExampleTypedHttpClient, AnotherExampleTypedHttpClient>(m_TeePeeBuilderOne, m_TeePeeBuilderTwo);

            // When
            var result = await controller.FireAndForget();

            // Then
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);

            requestTrackerOne.WasCalled(1);
            requestTrackerTwo.WasCalled(1);
        }

        #endregion

        private static T ResolveWithTypedClient<T, TClient1, TClient2>(TeePeeBuilder<TClient1> teePeeBuilder1, TeePeeBuilder<TClient2> teePeeBuilder2, Action<IServiceCollection> setup = null) where T : class 
                                                                                                                                                                                                where TClient1 : class
                                                                                                                                                                                                where TClient2 : class
        {
            var serviceCollection = new ServiceCollection();

            if (setup != null)
            {
                // Use your own Typed Client setup here
                setup(serviceCollection);
                serviceCollection.AddTrackingForTypedClient(typeof(TClient1), teePeeBuilder1.Build().HttpHandler);
                serviceCollection.AddTrackingForTypedClient(typeof(TClient2), teePeeBuilder2.Build().HttpHandler);
            }
            else
            {
                var teePeeMessageHandler1 = teePeeBuilder1.Build().HttpHandler;
                serviceCollection.AddHttpClient<TClient1>(_ => { })
                                 .AddHttpMessageHandler(_ => teePeeMessageHandler1);
                
                var teePeeMessageHandler2 = teePeeBuilder2.Build().HttpHandler;
                serviceCollection.AddHttpClient<TClient2>(_ => { })
                                 .AddHttpMessageHandler(_ => teePeeMessageHandler2);
            }

            serviceCollection.AddTransient<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }
    }
}