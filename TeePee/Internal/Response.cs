using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;

namespace TeePee.Internal
{
    internal class Response
    {
        private readonly HttpStatusCode m_ResponseStatusCode;
        
        private readonly TeePeeOptions m_Options;

        private readonly object? m_ResponseBody;
        private readonly HttpContent? m_ResponseBodyContent;
        private readonly string? m_ResponseBodyMediaType;
        private readonly string? m_ResponseBodyEncoding; 

        private readonly ReadOnlyDictionary<string, string> m_ResponseHeaders;

        public Response(HttpStatusCode responseStatusCode, TeePeeOptions options, object? responseBody, HttpContent? responseBodyContent, string? responseBodyMediaType, string? responseBodyEncoding, IDictionary<string, string> responseHeaders)
        {
            m_ResponseStatusCode = responseStatusCode;

            m_Options = options;

            m_ResponseBody = responseBody;
            m_ResponseBodyContent = responseBodyContent;
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
            if (m_ResponseBody == null && m_ResponseBodyContent == null)
                return null;

            if (m_ResponseBody == null)
                return m_ResponseBodyContent; // Note: Can only be used once as Dispose will dispose it and we can't create another instance from this

            var jsonContent = JsonContent.Create(m_ResponseBody, new(m_ResponseBodyMediaType), m_Options.ResponseBodySerializerOptions);
            if (m_ResponseBodyEncoding != null)
                jsonContent.Headers.ContentType.CharSet = m_ResponseBodyEncoding;

            return jsonContent;
        }
    }
}
