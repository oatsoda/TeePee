using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TeePee.Refit
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Lazy<MethodInfo> s_RefitGetHttpClientFactoryNameMethod = new(() =>
        {
#pragma warning disable 219 //Force assembly reference to Refit
            var a = nameof(global::Refit.HttpClientFactoryExtensions.AddRefitClient);
#pragma warning restore 219
            var type = Type.GetType("Refit.UniqueName, Refit")!;
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "ForType" && !m.IsGenericMethod);
        });

        private static readonly Dictionary<Type, string> s_HttpClientFactoryNames = new();

        private static string GetRefitHttpFactoryName<T>()
        {
            var type = typeof(T);
            if (s_HttpClientFactoryNames.TryGetValue(type, out var name))
                return name;

            name = (string)s_RefitGetHttpClientFactoryNameMethod.Value.Invoke(null, new object[] { type });
            s_HttpClientFactoryNames[type] = name;
            return name;
        }

        public static IServiceCollection AttachToRefitInterface<TRefitInterface>(this IServiceCollection serviceCollection, TeePee teePee)
        {
            // Get Delegating Handler to inject into the Http pipeline
            var requestHandler = teePee.HttpHandler;
            serviceCollection.AddTransient(_ => requestHandler);

            // Reflection: Get the registered HttpClient Name as used in Refit AddRefitClient (internal UniqueName helper class)
            var httpClientFactoryName = GetRefitHttpFactoryName<TRefitInterface>();
            
            // Add same-named HttpClient Name into same ServiceCollection which should allow us to append to existing options on that Http Named instance
            serviceCollection
                .AddHttpClient(httpClientFactoryName)
                .AddHttpMessageHandler(_ => requestHandler);

            return serviceCollection;
        }
    }
}
