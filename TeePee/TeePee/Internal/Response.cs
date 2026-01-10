using System.Collections.ObjectModel;
using System.Net;
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
            if (responseBodyEncoding != null && responseBodyMediaType == null)
                throw new ArgumentNullException(nameof(responseBodyMediaType), "Response Body MediaType must be specified if Encoding is specified.");

            m_ResponseStatusCode = responseStatusCode;

            m_Options = options;

            m_ResponseBody = responseBody;
            m_ResponseBodyContent = responseBodyContent;
            m_ResponseBodyMediaType = responseBodyMediaType;
            m_ResponseBodyEncoding = responseBodyEncoding;

            m_ResponseHeaders = new(responseHeaders);
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

            System.Net.Http.Headers.MediaTypeHeaderValue? contentType = null;
            if (m_ResponseBodyMediaType != null)
            {
                contentType = new(m_ResponseBodyMediaType);
            
                if (m_ResponseBodyEncoding != null)
                    contentType.CharSet = m_ResponseBodyEncoding;
            }

            return JsonContent.Create(m_ResponseBody, contentType, m_Options.ResponseBodySerializerOptions);
        }
    }
}
