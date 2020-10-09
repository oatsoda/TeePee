using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TeePee.Examples.WebApp.Controllers;
using Xunit;

namespace TeePee.Examples.WebApp.Tests
{
    public class HttpClientFactoryBasicUsageControllerTests
    {
        private readonly TeePeeBuilder m_TeePeeBuilder = new TeePeeBuilder();

        #region Manual Injection

        [Fact]
        public async Task RecommendedPassiveMocking()
        {
            // Given
            m_TeePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Get)
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

            var controller = new HttpClientFactoryBasicUsageController(m_TeePeeBuilder.Build().CreateHttpClientFactory());

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
            var requestTracker = m_TeePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Put)
                                                .ContainingQueryParam("filter", "other")
                                                .WithBody(new { Caller = "ThisCaller" })
                                                .Responds()
                                                .WithStatus(HttpStatusCode.Created)
                                                .TrackRequest();

            var controller = new HttpClientFactoryBasicUsageController(m_TeePeeBuilder.Build().CreateHttpClientFactory());

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
            m_TeePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Get)
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

            var controller = Resolve<HttpClientFactoryBasicUsageController>(m_TeePeeBuilder);

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
            var requestTracker = m_TeePeeBuilder.ForRequest("https://some.api/path/resource", HttpMethod.Put)
                                                .ContainingQueryParam("filter", "other")
                                                .WithBody(new { Caller = "ThisCaller" })
                                                .Responds()
                                                .WithStatus(HttpStatusCode.Created)
                                                .TrackRequest();
            
            var controller = Resolve<HttpClientFactoryBasicUsageController>(m_TeePeeBuilder);

            // When
            var result = await controller.FireAndForget();

            // Then
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);

            requestTracker.WasCalled(1);
        }

        #endregion

        private static T Resolve<T>(TeePeeBuilder teePeeBuilder, Action<IServiceCollection> additionalConfiguration = null) where T : class
        {
            var serviceCollection = new ServiceCollection();
            additionalConfiguration?.Invoke(serviceCollection);

            var teePeeMessageHandler = teePeeBuilder.Build().HttpHandler;

            serviceCollection.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName)
                             .AddHttpMessageHandler(_ => teePeeMessageHandler);
            
            serviceCollection.AddTransient<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }
    }
}
