using Microsoft.Extensions.DependencyInjection;
using Refit;
//using TeePee.UsageExtensions;

namespace TeePee.Refit
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AttachToRefitInterface<TRefitInterface>(this IServiceCollection serviceCollection, TeePeeBuilder teePeeBuilder)
            where TRefitInterface : class
        {
            var temp = teePeeBuilder.Build().GetAwaiter().GetResult();

            serviceCollection
                .AddRefitClient<TRefitInterface>() // This should continue configuring the same Refit client
                .AddHttpMessageHandler(_ => temp.HttpHandler)
                ;//.AddSingletonTeePeeMessageHandler(teePeeBuilder);

            return serviceCollection;
        }
    }
}
