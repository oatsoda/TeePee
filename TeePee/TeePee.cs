using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using TeePee.Extensions;
using TeePee.Internal;

namespace TeePee
{
    public class TeePee
    {
        internal string HttpClientNamedInstance { get; }

        public TeePeeMessageHandler HttpHandler { get; }
        
        internal TeePee(string httpClientNamedInstance, TeePeeMode mode, List<RequestMatchRule> matches, HttpStatusCode unmatchedStatusCode, string unmatchedBody, ILogger logger)
        {
            HttpClientNamedInstance = httpClientNamedInstance;
            HttpHandler = new TeePeeMessageHandler(mode, matches, 
                                                         () => new HttpResponseMessage(unmatchedStatusCode)
                                                               {
                                                                   Content = unmatchedBody == null 
                                                                                 ? null 
                                                                                 : new StringContent(unmatchedBody)
                                                               },
                                                         logger
                                                         );
        }
        
        /*
         * The CreateClient / Create HttpClientFactory only needed for Manual Injection
         */

        public ManualTeePee Manual(string baseAddressForHttpClient = null)
        {
            return new ManualTeePee(this, baseAddressForHttpClient);
        }

        public class ManualTeePee
        {
            private readonly Uri m_BaseAddressForHttpClient;

            public TeePee TeePee { get; }

            internal ManualTeePee(TeePee teePee, string baseAddressForHttpClient)
            {
                m_BaseAddressForHttpClient = baseAddressForHttpClient == null ? null : new Uri(baseAddressForHttpClient);
                TeePee = teePee;
            }

            public HttpClient CreateClient()
            {
                return m_BaseAddressForHttpClient == null 
                           ? new HttpClient(TeePee.HttpHandler) 
                           : new HttpClient(TeePee.HttpHandler) { BaseAddress = m_BaseAddressForHttpClient };
            }

            public IHttpClientFactory CreateHttpClientFactory() => new WrappedHttpClientFactory(CreateClient(), TeePee.HttpClientNamedInstance);

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
    }
    
    /*
     * This stuff only needed for Manual Injection
     */

    public static class ManualTeePeeBuilderExtensions
    {
        public static IHttpClientFactory ToHttpClientFactory(this IEnumerable<TeePee.ManualTeePee> teePees)
        {
            var factory = new TeePeeNamedClientsHttpClientFactory();
            foreach (var teePee in teePees)
                factory.Add(teePee.TeePee.HttpClientNamedInstance, teePee.CreateClient());

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
