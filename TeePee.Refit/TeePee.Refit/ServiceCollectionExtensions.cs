using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace TeePee.Refit
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AttachToRefitInterface<TRefitInterface>(this IServiceCollection serviceCollection, TeePee teePee)
            where TRefitInterface : class
        {
            // Get Delegating Handler to inject into the Http pipeline
            var requestHandler = teePee.HttpHandler;
            serviceCollection.AddTransient(_ => requestHandler);

            serviceCollection.AddRefitClient<TRefitInterface>() // This should continue configuring the same Refit client
                .AddHttpMessageHandler(_ => requestHandler)
                ;

            return serviceCollection;
        }
    }
}
