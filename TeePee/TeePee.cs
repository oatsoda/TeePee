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
        }
        
        public HttpClient CreateClient() => new HttpClient(HttpHandler);
        public IHttpClientFactory CreateHttpClientFactory() => new WrappedHttpClientFactory(CreateClient());

        private class WrappedHttpClientFactory : IHttpClientFactory
        {
            private readonly HttpClient m_HttpClient;

            internal WrappedHttpClientFactory(HttpClient httpClient)
            {
                m_HttpClient = httpClient;
            }

            public HttpClient CreateClient(string name)
            {
                return m_HttpClient;
            }
        }
    }
}