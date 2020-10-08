using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using TeePee.Examples.WebApp.Controllers;
using Xunit;

namespace TeePee.Examples.WebApp.Tests
{
    public class HttpClientFactoryTypedUsageControllerTests
    {
        private readonly TeePeeBuilder m_TeePeeBuilder = new TeePeeBuilder();

        #region Manual Injection
        
        [Fact]
        public async Task ManualInjection_RecommendedPassiveMocking()
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

            var controller = new HttpClientFactoryTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilder.Build().CreateClient()));

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
                                                .ContainingQueryParam("filter", "other")
                                                .WithBody(new { Caller = "ThisCaller" })
                                                .Responds()
                                                .WithStatus(HttpStatusCode.Created)
                                                .TrackRequest();

            var controller = new HttpClientFactoryTypedUsageController(new ExampleTypedHttpClient(m_TeePeeBuilder.Build().CreateClient()));

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

            var controller = ResolveWithTypedClient<HttpClientFactoryTypedUsageController, ExampleTypedHttpClient>(m_TeePeeBuilder, sc =>
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

            var controller = ResolveWithTypedClient<HttpClientFactoryTypedUsageController, ExampleTypedHttpClient>(m_TeePeeBuilder);

            // When
            var result = await controller.FireAndForget();

            // Then
            Assert.NotNull(result);
            Assert.IsType<OkResult>(result);

            requestTracker.WasCalled(1);
        }

        #endregion

        private static T ResolveWithTypedClient<T, TClient>(TeePeeBuilder teePeeBuilder, Action<IServiceCollection> setup = null) where T : class where TClient : class
        {
            var serviceCollection = new ServiceCollection();

            var teePeeMessageHandler = teePeeBuilder.Build().HttpHandler;

            if (setup != null)
            {
                // Use your own Typed Client setup here
                setup(serviceCollection); 
                serviceCollection.AddTrackingForTypedClient<TClient>(teePeeMessageHandler);
            }
            else
            {
                serviceCollection.AddHttpClient<TClient>(_ => { })
                                 .AddHttpMessageHandler(_ => teePeeMessageHandler);;
            }

            serviceCollection.AddTransient<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }
    }

    internal static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Unfortunately you have to know the Type of the Typed Http Client in order to be able to track it.  Therefore this means exposing internal implementation details
        /// into your tests.
        /// </summary>
        internal static IServiceCollection AddTrackingForTypedClient<T>(this IServiceCollection serviceCollection, TeePeeMessageHandler teePeeMessageHandler)
        {
            // Note that you cannot nest/chain Delegating Handlers with HttpClientFactory pattern as it must not have any state as it re-uses instances etc.

            // Reflection: Get the registered HttpClientFactory Name
#pragma warning disable 219 //Force assembly reference to Microsoft.Extensions.Http
            var a = nameof(Microsoft.Extensions.Http.HttpClientFactoryOptions);
#pragma warning restore 219
            var type = Type.GetType("Microsoft.Extensions.Internal.TypeNameHelper, Microsoft.Extensions.Http");
            var method = type.GetMethod("GetTypeDisplayName", BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, new [] { typeof(Type), typeof(bool), typeof(bool), typeof(bool), typeof(char) }, null);
            var httpClientFactoryName = (string)method.Invoke(null, new [] { (object)typeof(T), false, false, true, '+' });

            // Reflection: Create an HttpClientBuilder using the same ServiceCollection and HttpClientFactoryName used within the Startup
            var builder = (IHttpClientBuilder)Activator.CreateInstance(Type.GetType("Microsoft.Extensions.DependencyInjection.DefaultHttpClientBuilder, Microsoft.Extensions.Http"), serviceCollection, httpClientFactoryName);

            // Add the Tracking DelegateHandler to the service collection via the HttpClientBuilder (but ensure registered in DI first)
            serviceCollection.AddTransient(_ => teePeeMessageHandler);
            builder.AddHttpMessageHandler(_ => teePeeMessageHandler);

            return serviceCollection;
        }
    }
}
