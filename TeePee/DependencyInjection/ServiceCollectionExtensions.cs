using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace TeePee.DependencyInjection
{
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
            var a = nameof(HttpClientFactoryOptions);
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