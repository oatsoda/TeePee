using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TeePee.Internal
{
    internal class Response
    {
        private readonly HttpStatusCode m_ResponseStatusCode;
        
        private readonly JsonSerializerOptions m_BodySerializeOptions;

        private readonly object? m_ResponseBody;
        private readonly string m_ResponseBodyMediaType;
        private readonly Encoding m_ResponseBodyEncoding; 

        private readonly ReadOnlyDictionary<string, string> m_ResponseHeaders;

        public Response(HttpStatusCode responseStatusCode, JsonSerializerOptions bodySerializeOptions, object? responseBody, string responseBodyMediaType, Encoding responseBodyEncoding, IDictionary<string, string> responseHeaders)
        {
            m_ResponseStatusCode = responseStatusCode;

            m_BodySerializeOptions = bodySerializeOptions;

            m_ResponseBody = responseBody;
            m_ResponseBodyMediaType = responseBodyMediaType;
            m_ResponseBodyEncoding = responseBodyEncoding;

            m_ResponseHeaders = new ReadOnlyDictionary<string, string>(responseHeaders);
        }
        
        internal HttpResponseMessage ToHttpResponseMessage()
        {
            var response = new HttpResponseMessage(m_ResponseStatusCode)
                           {
                               Content = BodyAsContent()
                           };
            foreach (var (name, value) in m_ResponseHeaders)
                response.Headers.Add(name, new[] { value });

            return response;
        }
        
        private HttpContent? BodyAsContent()
        {
            if (m_ResponseBody == null)
                return null;

            var serialisedResponseBody = JsonSerializer.Serialize(m_ResponseBody, m_BodySerializeOptions);
            
            // TODO: Non string? Multipart/FormUrl
            return new StringContent(serialisedResponseBody, m_ResponseBodyEncoding, m_ResponseBodyMediaType);
        }
    }
}