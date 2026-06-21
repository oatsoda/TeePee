using System.Net;
using TeePee.Internal;

namespace TeePee
{
    public class TeePee
    {
        //public TeePeeMessageHandler HttpHandler { get; }

        internal IReadOnlyList<RequestMatchRule> MatchRules { get; }
        internal HttpStatusCode UnmatchedStatusCode { get; }
        internal string? UnmatchedBody { get; }

        internal TeePee(IReadOnlyList<RequestMatchRule> matchRules,
                        HttpStatusCode unmatchedStatusCode,
                        string? unmatchedBody)
        {
            MatchRules = matchRules;
            UnmatchedStatusCode = unmatchedStatusCode;
            UnmatchedBody = unmatchedBody;

            //HttpHandler = new(
            //    options,
            //    matchRules,
            //    () => new(unmatchedStatusCode)
            //    {
            //        Content = unmatchedBody == null
            //        ? null
            //        : new StringContent(unmatchedBody)
            //    },
            //    logger
            //);
        }

        //internal void Reset(
        //    IReadOnlyList<RequestMatchRule> requestMatchRules,
        //    HttpStatusCode unmatchedStatusCode,
        //     string? unmatchedBody)
        //{
        //    HttpHandler.Reset(requestMatchRules,
        //        // TODO: Dupe. Do we need TeePee layer, or could this all be in Handler ctor? Depends how I plan to do Manual stuff.
        //        () => new(unmatchedStatusCode)
        //        {
        //            Content = unmatchedBody == null
        //            ? null
        //            : new StringContent(unmatchedBody)
        //        });
        //}

        /*
         * The CreateClient / Create HttpClientFactory only needed for Manual Injection
         */

        // TODO: Think about re-modelling this. Builder.Build() response could be the manual. HttpHandler
        // may live somewhere else - possibly as state on the Builder, to allow reset/re-use.

        //public ManualTeePee Manual(TeePeeBuilder teePeeBuilder, string? baseAddressForHttpClient = null)
        //{
        //    return new(this, baseAddressForHttpClient);
        //}

        public class ManualTeePee
        {
            private readonly TeePeeBuilder m_Builder;
            private readonly Uri? m_BaseAddressForHttpClient;

            internal ManualTeePee(TeePeeBuilder teePeeBuilder, string? baseAddressForHttpClient)
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

    /*
     * This stuff only needed for Manual Injection
     */

    //public static class ManualTeePeeBuilderExtensions
    //{
    //    public static IHttpClientFactory ToHttpClientFactory(this IEnumerable<TeePee.ManualTeePee> teePees)
    //    {
    //        var factory = new TeePeeNamedClientsHttpClientFactory();
    //        foreach (var teePee in teePees)
    //            factory.Add(teePee.TeePee.HttpClientNamedInstance, teePee.CreateClient());

    //        return factory;
    //    }

    //    internal class TeePeeNamedClientsHttpClientFactory : IHttpClientFactory
    //    {
    //        private readonly Dictionary<string, HttpClient> m_NamedClients = [];

    //        internal void Add(string? namedInstance, HttpClient httpClient)
    //        {
    //            namedInstance ??= Microsoft.Extensions.Options.Options.DefaultName;
    //            m_NamedClients.Add(namedInstance, httpClient);
    //        }

    //        public HttpClient CreateClient(string name)
    //        {
    //            // Force callers to specify correct named instance
    //            return name != null! && m_NamedClients.ContainsKey(name)
    //                ? m_NamedClients[name]
    //                : throw new ArgumentOutOfRangeException(nameof(name), $"No HttpClients configured with name '{name}'. Configured with {m_NamedClients.Keys.Select(k => $"'{k}'").Flat()}.");
    //        }
    //    }
    //}
}
