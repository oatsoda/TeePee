using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace TeePee.Examples.WebApp.Tests
{
    public static class Resolve
    {
        public static T WithDefaultClient<T>(TeePeeBuilder teePeeBuilder, Action<IServiceCollection> additionalConfiguration = null) where T : class
        {
            var serviceCollection = new ServiceCollection();
            additionalConfiguration?.Invoke(serviceCollection);

            var teePeeMessageHandler = teePeeBuilder.Build().HttpHandler;

            serviceCollection.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName)
                             .AddHttpMessageHandler(_ => teePeeMessageHandler);
            
            serviceCollection.AddTransient<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }

        public static T WithNamedClients<T>(Action<IServiceCollection> configureServices = null, params TeePeeBuilder[] teePeeBuilders) where T : class
        {
            var serviceCollection = new ServiceCollection();

            // Note: Unfortunately the Basic usage Resolve method will allow ANY named instances to work - therefore a limitation

            foreach (var teePeeBuilder in teePeeBuilders)
            {
                var teePee = teePeeBuilder.Build();

                if (configureServices != null)
                {
                    // Use your own Named Client setup here
                    configureServices(serviceCollection);
                    serviceCollection.CheckNamedClientIsRegistered(teePee.HttpClientNamedInstance); // Explicit check that the Named client was registered in the callers own registration, else following code will add the handler :(
                    serviceCollection.GetHttpClientBuilderFor(teePee.HttpClientNamedInstance) 
                                     .AddHttpMessageHandler(_ => teePee.HttpHandler);
                }
                else
                {
                    // Otherwise, register the Named Client here
                    serviceCollection.AddHttpClient(teePee.HttpClientNamedInstance)
                                     .AddHttpMessageHandler(_ => teePee.HttpHandler);
                }
            }

            serviceCollection.AddTransient<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }


        public static T WithTypedClient<T, TClient>(TeePeeBuilder teePeeBuilder, Action<IServiceCollection> configureServices = null) where T : class where TClient : class
        {
            var serviceCollection = new ServiceCollection();

            var teePeeMessageHandler = teePeeBuilder.Build().HttpHandler;

            if (configureServices != null)
            {
                // Use your own Typed Client setup here
                configureServices(serviceCollection);
                serviceCollection.AddTrackingForTypedClient<TClient>(teePeeMessageHandler);
            }
            else
            {
                // Otherwise, register the Typed Client here
                serviceCollection.AddHttpClient<TClient>(_ => { })
                                 .AddHttpMessageHandler(_ => teePeeMessageHandler);
            }

            serviceCollection.AddTransient<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }
        
        public static T WithTypedClients<T, TClient1, TClient2>(TeePeeBuilder<TClient1> teePeeBuilder1, TeePeeBuilder<TClient2> teePeeBuilder2, Action<IServiceCollection> setup = null) where T : class 
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

    internal static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Unfortunately you have to know the Type of the Typed Http Client in order to be able to track it.  Therefore this means exposing internal implementation details
        /// into your tests.
        /// </summary>
        internal static IServiceCollection AddTrackingForTypedClient<T>(this IServiceCollection serviceCollection, TeePeeMessageHandler teePeeMessageHandler)
        {
            return serviceCollection.AddTrackingForTypedClient(typeof(T), teePeeMessageHandler);
        }

        internal static IServiceCollection AddTrackingForTypedClient(this IServiceCollection serviceCollection, Type typedClientType, TeePeeMessageHandler teePeeMessageHandler)
        {
            // Note that you cannot nest/chain Delegating Handlers with HttpClientFactory pattern as it must not have any state as it re-uses instances etc.

            // Reflection: Get the registered HttpClientFactory Name
#pragma warning disable 219 //Force assembly reference to Microsoft.Extensions.Http
            var a = nameof(Microsoft.Extensions.Http.HttpClientFactoryOptions);
#pragma warning restore 219
            var type = Type.GetType("Microsoft.Extensions.Internal.TypeNameHelper, Microsoft.Extensions.Http");
            var method = type.GetMethod("GetTypeDisplayName", BindingFlags.Static | BindingFlags.Public, null, CallingConventions.Any, new[] { typeof(Type), typeof(bool), typeof(bool), typeof(bool), typeof(char) }, null);
            var httpClientNamedInstance = (string)method.Invoke(null, new[] { (object)typedClientType, false, false, true, '+' });

            // Reflection: Create an HttpClientBuilder using the same ServiceCollection and HttpClientFactoryName used within the Startup
            var builder = serviceCollection.GetHttpClientBuilderFor(httpClientNamedInstance);

            // Add the Tracking DelegateHandler to the service collection via the HttpClientBuilder (but ensure registered in DI first)
            serviceCollection.AddTransient(_ => teePeeMessageHandler);
            builder.AddHttpMessageHandler(_ => teePeeMessageHandler);

            return serviceCollection;
        }

        internal static IHttpClientBuilder GetHttpClientBuilderFor(this IServiceCollection serviceCollection, string httpClientNamedInstance)
        {
            // Service Collection must be the same one that was used to originally register the type.
            var defaultHttpClientBuilderType = Type.GetType("Microsoft.Extensions.DependencyInjection.DefaultHttpClientBuilder, Microsoft.Extensions.Http");
            return (IHttpClientBuilder)Activator.CreateInstance(defaultHttpClientBuilderType, serviceCollection, httpClientNamedInstance);
        }

        
        internal static void CheckNamedClientIsRegistered(this IServiceCollection serviceCollection, string httpClientNamedInstance)
        {
            var namedClientOptionsServices = serviceCollection.Select(s => s.ImplementationInstance as ConfigureNamedOptions<HttpClientFactoryOptions>)
                                                              .Where(o => o != null && o.Name == httpClientNamedInstance)
                                                              .ToList();

            if (!namedClientOptionsServices.Any())
                throw new InvalidOperationException($"No registration found for Named Client '{httpClientNamedInstance}'. {namedClientOptionsServices.Count} registrations found [{string.Join(',', namedClientOptionsServices.Select(o => $"'{o.Name}'"))}].");
        }
    }
}