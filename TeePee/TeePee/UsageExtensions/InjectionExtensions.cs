using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using TeePee.Built;

namespace TeePee.UsageExtensions
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AttachToDefaultClient(this IServiceCollection services, TeePeeBuilder teePeeBuilder)
        {
            return AttachToNamedClientInternal(services, teePeeBuilder, Options.DefaultName);
        }

        public static IServiceCollection AttachToTypedClient<TClient>(this IServiceCollection services, TeePeeBuilder teePeeBuilder)
        {
            return AttachToNamedClientInternal(services, teePeeBuilder, typeof(TClient).Name!);
        }

        public static IServiceCollection AttachToNamedClient(this IServiceCollection services, TeePeeBuilder teePeeBuilder, string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
            {
                throw new ArgumentException("Cannot attached to a Named client without a Name.");
            }

            return AttachToNamedClientInternal(services, teePeeBuilder, clientName);
        }

        public static IServiceCollection AttachToNamedClientInternal(this IServiceCollection services, TeePeeBuilder teePeeBuilder, string clientName)
        {
            // We expect this to be called only once per Builder? Per-Fixture is expected; Per-Test, would you
            // be using DI? Maybe but a new Bulder + Service Collection would be created per test - so isolated.
            // So YES, the Builder would expect to be "attached" only once and not expect a TeePeeMessageHandler to already exist.

            // TODO: So I would need to enforce NOT being able to call this twice?

            // register the test handler in DI - DO I NEED TO? Will be problematic with multiple TeePeeBuilders for multiple named/typed clients in SUT.
            //services.AddTransient<THandler>();

            // inject the handler into the existing named client configuration
            services.Configure<HttpClientFactoryOptions>(clientName, options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                {
                    // resolve handler from the builder's IServiceProvider and add it to the pipeline
                    //var handler = (DelegatingHandler)builder.Services.GetRequiredService<THandler>();

                    //var handler = teePeeBuilder.Build().GetAwaiter().GetResult().HttpHandler;
                    var handler = new TeePeeMessageHandler(teePeeBuilder);

                    // Add a per-pipeline wrapper (must be new and have InnerHandler == null here)
                    // so when the pipeline is disposed the wrapper ignores disposing the inner handler.
                    builder.AdditionalHandlers.Add(new NonDisposableDelegatingHandler());

                    // Add the actual TeePee handler (also must be a freshly created DelegatingHandler)
                    builder.AdditionalHandlers.Add(handler);
                    // TODO: Should we dispose somewhere? Maybe Builder should be disposed on Fixture dispose?
                });
            });

            return services;
        }

        public sealed class NonDisposableDelegatingHandler : DelegatingHandler
        {
            public NonDisposableDelegatingHandler()
            {
            }

            // Intentionally suppress disposing the inner handler.
            protected override void Dispose(bool disposing)
            {
                // No-op: do NOT call base.Dispose(disposing) so InnerHandler is not disposed here.
            }
        }
    }
}
