using System.Net;

namespace TeePee.Built
{
    public class TeePeeSeeded
    {
        internal IReadOnlyList<RequestMatchRule> MatchRules { get; }
        internal HttpStatusCode UnmatchedStatusCode { get; }
        internal string? UnmatchedBody { get; }

        internal TeePeeSeeded(IReadOnlyList<RequestMatchRule> matchRules,
                        HttpStatusCode unmatchedStatusCode,
                        string? unmatchedBody)
        {
            MatchRules = matchRules;
            UnmatchedStatusCode = unmatchedStatusCode;
            UnmatchedBody = unmatchedBody;
        }
    }

    // TODO: Think about re-modelling this. Builder.Build() response could be the manual. HttpHandler
    // may live somewhere else - possibly as state on the Builder, to allow reset/re-use.

    public class TeePeeManual
    {
        private readonly TeePeeBuilder m_Builder;
        private readonly Uri? m_BaseAddressForHttpClient;

        internal TeePeeManual(TeePeeBuilder teePeeBuilder, string? baseAddressForHttpClient)
        {
            m_Builder = teePeeBuilder;
            m_BaseAddressForHttpClient = baseAddressForHttpClient == null ? null : new Uri(baseAddressForHttpClient);
        }

        public HttpClient CreateClient()
        {
            var handler = new TeePeeMessageHandler(m_Builder);
            return m_BaseAddressForHttpClient == null
                ? new(handler)
                : new HttpClient(handler) { BaseAddress = m_BaseAddressForHttpClient };
        }

        public IHttpClientFactory CreateHttpClientFactory(string? clientName) => new WrappedHttpClientFactory(CreateClient(), clientName);

        private class WrappedHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient m_HttpClient;
            private readonly string m_NamedInstance;

            internal WrappedHttpClientFactory(HttpClient httpClient, string? namedInstance)
            {
                m_HttpClient = httpClient;
                m_NamedInstance = namedInstance ?? Microsoft.Extensions.Options.Options.DefaultName; // Default value used by actual HttpClientFactoryExtensions.CreateClient();
            }

            public HttpClient CreateClient(string name)
            {
                // Force callers to specify correct named instance
                return m_NamedInstance == name
                    ? m_HttpClient
                    : throw new ArgumentOutOfRangeException(nameof(name), $"No HttpClients configured with name '{name}'. Configured with '{m_NamedInstance}'.");
            }
        }
    }
}
