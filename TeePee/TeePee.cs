using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using TeePee.Internal;

namespace TeePee
{
    public class TeePee
    {
        // TODO: Only public for Refit assembly
        public TeePeeMessageHandler HttpHandler { get; }
        
        internal TeePee(List<RequestMatch> matches, HttpStatusCode unmatchedStatusCode, string unmatchedBody)
        {
            HttpHandler = new TeePeeMessageHandler(matches, 
                                                         () => new HttpResponseMessage(unmatchedStatusCode)
                                                               {
                                                                   Content = unmatchedBody == null 
                                                                                 ? null 
                                                                                 : new StringContent(unmatchedBody)
                                                               });

            // TODO: Build clients here rather than methods
        }
        
        public HttpClient CreateClient() => new HttpClient(HttpHandler);
        public IHttpClientFactory CreateHttpClientFactory(string namedInstance = null) => new WrappedHttpClientFactory(CreateClient(), namedInstance); // <!-- change named instant to be para to TeePee builder?

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