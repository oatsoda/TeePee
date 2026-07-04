using Microsoft.Extensions.DependencyInjection;
using Refit;
using TeePee.UsageExtensions;

namespace TeePee.Refit
{
    public static class ServiceCollectionExtensions
    {
        //public static IServiceCollection AttachToRefitInterface<TRefitInterface>(this IServiceCollection serviceCollection, TeePeeBuilder teePeeBuilder)
        //    where TRefitInterface : class
        //{
        //    // Get Delegating Handler to inject into the Http pipeline
        //    var requestHandler = new TeePeeMessageHandler(teeP);
        //    serviceCollection.AddTransient(_ => requestHandler);

        //    serviceCollection.AddRefitClient<TRefitInterface>() // This should continue configuring the same Refit client
        //        .AddHttpMessageHandler(_ => requestHandler)
        //        ;

        //    return serviceCollection;
        //}

        public static IServiceCollection AttachToRefitInterface<TRefitInterface>(this IServiceCollection serviceCollection, TeePeeBuilder teePeeBuilder)
            where TRefitInterface : class
        {
            serviceCollection
                .AddRefitClient<TRefitInterface>() // This should continue configuring the same Refit client
                .AddSingletonTeePeeMessageHandler(teePeeBuilder);

            return serviceCollection;
        }
    }
}
