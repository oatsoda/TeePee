using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;

namespace TeePee.Internal
{
    internal class Response
    {
        private readonly HttpStatusCode m_ResponseStatusCode;
        private readonly HttpContent m_ResponseBody;
        private readonly ReadOnlyDictionary<string, string> m_ResponseHeaders;

        public Response(HttpStatusCode responseStatusCode, HttpContent responseBody, IDictionary<string, string> responseHeaders)
        {
            m_ResponseStatusCode = responseStatusCode;
            m_ResponseBody = responseBody;
            m_ResponseHeaders = new ReadOnlyDictionary<string, string>(responseHeaders);
        }
        
        internal HttpResponseMessage ToHttpResponseMessage()
        {
            var response = new HttpResponseMessage(m_ResponseStatusCode)
                           {
                               Content = m_ResponseBody
                           };
            foreach (var (name, value) in m_ResponseHeaders)
                response.Headers.Add(name, new[] { value });

            return response;
        }
    }
}