using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using TeePee.Examples.WebApp.Controllers;
using Xunit;

namespace TeePee.Examples.WebApp.Tests
{
    public class HttpClientFactoryNamedUsageControllerTests
    {
        private readonly TeePeeBuilder m_TeePeeBuilder = new TeePeeBuilder();
        private const string _NAMED_HTTP_CLIENT = "ThirdPartyApi";

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

            var controller = new HttpClientFactoryNamedUsageController(m_TeePeeBuilder.Build().CreateHttpClientFactory(_NAMED_HTTP_CLIENT));

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

            var controller = new HttpClientFactoryNamedUsageController(m_TeePeeBuilder.Build().CreateHttpClientFactory(_NAMED_HTTP_CLIENT));

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
            
            var controller = Resolve<HttpClientFactoryNamedUsageController>(m_TeePeeBuilder.Build());

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
            
            var controller = Resolve<HttpClientFactoryNamedUsageController>(m_TeePeeBuilder.Build());

            // When
            var result = await controller.FireAndForget();

            // Then
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);

            requestTracker.WasCalled(1);
        }

        #endregion
        
        private static T Resolve<T>(TeePee teePee, Action<IServiceCollection> setup = null) where T : class
        {
            var serviceCollection = new ServiceCollection();

            var teePeeMessageHandler = teePee.HttpHandler;

            // Unfortunately the Basic usage Resolve method will allow ANY named instances to work - therefore a limitation in 

            serviceCollection.AddHttpClient(_NAMED_HTTP_CLIENT)
                             .AddHttpMessageHandler(_ => teePeeMessageHandler);
            
            //serviceCollection.AddTransient<HttpMessageHandlerBuilder>(_ => new NamedUsageHttpMessageHandlerBuilder(teePeeMessageHandler, ""));
            
            serviceCollection.AddTransient<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }

        //internal class NamedUsageHttpMessageHandlerBuilder : HttpMessageHandlerBuilder
        //{
        //    private readonly TeePeeMessageHandler m_MessageHandler;

        //    public override string Name { get; set; }
        //    public override HttpMessageHandler PrimaryHandler { get; set; }
        //    public override IList<DelegatingHandler> AdditionalHandlers { get; } = new List<DelegatingHandler>();

        //    public NamedUsageHttpMessageHandlerBuilder(TeePeeMessageHandler messageHandler, string namedClient)
        //    {
        //        Name = namedClient;
        //        m_MessageHandler = messageHandler;
        //    }

        //    public override HttpMessageHandler Build()
        //    {
        //        return m_MessageHandler;
        //    }

        //}
    }
}
