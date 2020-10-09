using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TeePee.Examples.WebApp.Controllers;
using Xunit;

namespace TeePee.Examples.WebApp.Tests
{
    // Basic usage of HttpClientFactory (i.e. non-named or type clients is only really meant as a refactoring step)
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

            var controller = Resolve.WithDefaultClient<HttpClientFactoryBasicUsageController>(m_TeePeeBuilder);

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
            
            var controller = Resolve.WithDefaultClient<HttpClientFactoryBasicUsageController>(m_TeePeeBuilder);

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
