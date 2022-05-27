using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace TeePee.Refit
{
    public static class ServiceCollectionExtensions
    {
        private static readonly Lazy<MethodInfo> s_RefitGetHttpClientFactoryNameMethod = new Lazy<MethodInfo>(() =>
        {
#pragma warning disable 219 //Force assembly reference to Refit
            var a = nameof(global::Refit.HttpClientFactoryExtensions.AddRefitClient);
#pragma warning restore 219
            var type = Type.GetType("Refit.UniqueName, Refit");
            return type.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(m => m.Name == "ForType" && !m.IsGenericMethod);
        });

        private static string GetRefitHttpFactoryName<T>() => (string)s_RefitGetHttpClientFactoryNameMethod.Value.Invoke(null, new object[] { typeof(T) });

        public static IServiceCollection AttachToRefitInterface<TRefitInterface>(this IServiceCollection serviceCollection, TeePee teePee)
        {
            // Get Delegating Handler to inject into the Http pipeline
            var requestHandler = teePee.HttpHandler;

            // Reflection: Get the registered HttpClientFactory Name as used in Refit AddRefitClient (internal UniqueName helper class)
            var httpClientFactoryName = GetRefitHttpFactoryName<TRefitInterface>();

            // Reflection: Create an HttpClientBuild using the same ServiceCollection and HttpClientFactoryName that the Refit was added to
            var builder = (IHttpClientBuilder)Activator.CreateInstance(Type.GetType("Microsoft.Extensions.DependencyInjection.DefaultHttpClientBuilder, Microsoft.Extensions.Http"), serviceCollection, httpClientFactoryName);

            // Add the Tracking DelegateHandler to the service collection via the HttpClientBuilder (but ensure registered in DI first)
            serviceCollection.AddTransient(_ => requestHandler);
            builder.AddHttpMessageHandler(_ => requestHandler);

            return serviceCollection;
        }
    }
}
