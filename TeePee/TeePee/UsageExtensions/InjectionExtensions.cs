using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using TeePee.Built;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace TeePee
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    public static class InjectionExtensions
    {
        public static IServiceCollection AttachToDefaultClient(this IServiceCollection services, TeePeeBuilder teePeeBuilder)
        {
            return AttachToNamedClientInternal(services, teePeeBuilder, AttachToClientType.Default, Options.DefaultName);
        }

        public static IServiceCollection AttachToTypedClient<TClient>(this IServiceCollection services, TeePeeBuilder teePeeBuilder)
        {
            return AttachToNamedClientInternal(services, teePeeBuilder, AttachToClientType.Typed, typeof(TClient).Name!);
        }

        public static IServiceCollection AttachToNamedClient(this IServiceCollection services, TeePeeBuilder teePeeBuilder, string clientName)
        {
            if (string.IsNullOrWhiteSpace(clientName))
            {
                throw new ArgumentException("Cannot attached to a Named client without a Name.");
            }

            return AttachToNamedClientInternal(services, teePeeBuilder, AttachToClientType.Named, clientName);
        }

        private static readonly ConcurrentDictionary<IServiceCollection, HashSet<string>> m_AttachedClientNames = new();

        private enum AttachToClientType
        {
            Default,
            Typed,
            Named
        }

        private static IServiceCollection AttachToNamedClientInternal(this IServiceCollection services,
            TeePeeBuilder teePeeBuilder, AttachToClientType type, string clientName)
        {
            // Validate the service collection hasn't been used to attach the same client already.
            var clientNames = m_AttachedClientNames.GetOrAdd(services, _ => []);
            if (clientNames.Contains(clientName))
            {
                switch (type)
                {
                    case AttachToClientType.Default:
                        throw new InvalidOperationException($"Already attached to Default Client");
                    case AttachToClientType.Typed:
                        throw new InvalidOperationException($"Already attached to Typed Client '{clientName}'");
                    case AttachToClientType.Named:
                        throw new InvalidOperationException($"Already attached to Named Client '{clientName}'");
                }
            }

            clientNames.Add(clientName);

            // inject the handler into the existing named client configuration
            services.Configure<HttpClientFactoryOptions>(clientName, options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                {
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
