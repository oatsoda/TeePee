using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

namespace TeePee.DependencyInjection
{
    public static class Resolve
    {
        public static T WithDefaultClient<T>(TeePeeBuilder teePeeBuilder) where T : class
        {
            var teePeeMessageHandler = teePeeBuilder.Build().HttpHandler;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddHttpClient(Options.DefaultName)
                             .AddHttpMessageHandler(_ => teePeeMessageHandler);
            
            serviceCollection.AddTransient<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }

        public static T WithNamedClients<T>(Action<IServiceCollection>? configureServices = null, params TeePeeBuilder[] teePeeBuilders) where T : class
        {
            var serviceCollection = new ServiceCollection();

            // Note: Unfortunately the Basic usage Resolve method will allow ANY named instances to work - therefore a limitation

            foreach (var teePeeBuilder in teePeeBuilders)
            {
                var teePee = teePeeBuilder.Build();

                if (teePee.HttpClientNamedInstance == null)
                    throw new InvalidOperationException($"'{nameof(WithNamedClients)}' requires the '{nameof(TeePee.HttpClientNamedInstance)}' to be set.");

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
        
        public static T WithTypedClient<T, TClient>(TeePeeBuilder teePeeBuilder, Action<IServiceCollection>? configureServices = null) where T : class where TClient : class
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
        
        public static T WithTypedClients<T, TClient1, TClient2>(TeePeeBuilder<TClient1> teePeeBuilder1, TeePeeBuilder<TClient2> teePeeBuilder2, Action<IServiceCollection>? setup = null) where T : class 
                                                                                                                                                                                          where TClient1 : class
                                                                                                                                                                                          where TClient2 : class
        {
            var serviceCollection = new ServiceCollection();
            
            var teePeeMessageHandler1 = teePeeBuilder1.Build().HttpHandler;
            var teePeeMessageHandler2 = teePeeBuilder2.Build().HttpHandler;

            if (setup != null)
            {
                // Use your own Typed Client setup here
                setup(serviceCollection);
                serviceCollection.AddTrackingForTypedClient<TClient1>(teePeeMessageHandler1);
                serviceCollection.AddTrackingForTypedClient<TClient2>(teePeeMessageHandler2);
            }
            else
            {
                serviceCollection.AddHttpClient<TClient1>(_ => { })
                                 .AddHttpMessageHandler(_ => teePeeMessageHandler1);
                serviceCollection.AddHttpClient<TClient2>(_ => { })
                                 .AddHttpMessageHandler(_ => teePeeMessageHandler2);
            }

            serviceCollection.AddTransient<T>();

            return serviceCollection.BuildServiceProvider().GetService<T>();
        }

        // TODO: More Generic versions of WithTypedClients
    }
}