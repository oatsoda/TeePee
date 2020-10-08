using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using TeePee.Extensions;
using TeePee.Internal;

namespace TeePee
{
    public class TeePee
    {
        // TODO: Public for Resolve methods in tests which need to be moved in-house
        public string HttpClientNamedInstance { get; }

        // TODO: Only public for Refit assembly
        public TeePeeMessageHandler HttpHandler { get; }
        
        internal TeePee(string httpClientNamedInstance, List<RequestMatch> matches, HttpStatusCode unmatchedStatusCode, string unmatchedBody)
        {
            HttpClientNamedInstance = httpClientNamedInstance;
            HttpHandler = new TeePeeMessageHandler(matches, 
                                                         () => new HttpResponseMessage(unmatchedStatusCode)
                                                               {
                                                                   Content = unmatchedBody == null 
                                                                                 ? null 
                                                                                 : new StringContent(unmatchedBody)
                                                               });
        }
        
        /*
         * The CreateClient / Create HttpClientFactory only needed for Manual Injection
         */

        public HttpClient CreateClient() => new HttpClient(HttpHandler);
        public IHttpClientFactory CreateHttpClientFactory() => new WrappedHttpClientFactory(CreateClient(), HttpClientNamedInstance);

        private class WrappedHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient m_HttpClient;
            private readonly string m_NamedInstance;

            internal WrappedHttpClientFactory(HttpClient httpClient, string namedInstance)
            {
                m_HttpClient = httpClient;
                m_NamedInstance = namedInstance ?? Microsoft.Extensions.Options.Options.DefaultName; // Default value used by actual HttpClientFactoryExtensions.CreateClient();
            }

            public HttpClient CreateClient(string name)
            {
                // Force callers to specify correct named instance
                return m_NamedInstance == name 
                           ? m_HttpClient
                           : throw new ArgumentOutOfRangeException($"No HttpClients configured with name '{name}'. Configured with '{m_NamedInstance}'.");
            }

        }
    }
    
    /*
     * This stuff only needed for Manual Injection
     */

    public static class TeePeeBuilderExtensions
    {
        public static IHttpClientFactory ToHttpClientFactory(this IEnumerable<TeePeeBuilder> builders)
        {
            var factory = new TeePeeNamedClientsHttpClientFactory();
            foreach (var teePeeBuilder in builders)
            {
                var teePee = teePeeBuilder.Build();
                factory.Add(teePee.HttpClientNamedInstance, teePee.CreateClient());
            }

            return factory;
        }
        
        internal class TeePeeNamedClientsHttpClientFactory : IHttpClientFactory
        {
            private readonly Dictionary<string, HttpClient> m_NamedClients = new Dictionary<string, HttpClient>();

            internal void Add(string namedInstance, HttpClient httpClient)
            {
                namedInstance ??= Microsoft.Extensions.Options.Options.DefaultName;
                m_NamedClients.Add(namedInstance, httpClient);
            }

            public HttpClient CreateClient(string name)
            {
                // Force callers to specify correct named instance
                return m_NamedClients.ContainsKey(name)
                           ? m_NamedClients[name]
                           : throw new ArgumentOutOfRangeException($"No HttpClients configured with name '{name}'. Configured with {m_NamedClients.Keys.Select(k => $"'{k}'").Flat()}.");
            }
        }
    }
}