using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using TeePee.Internal;

namespace TeePee
{
    public class ResponseBuilder
    {
        private readonly RequestMatchBuilder m_RequestMatchBuilder;
        private readonly JsonSerializerOptions m_BodySerializeOptions;

        private HttpStatusCode m_ResponseStatusCode = HttpStatusCode.NoContent;

        private object m_ResponseBody;
        private string m_ResponseBodyMediaType;
        private Encoding m_ResponseBodyEncoding; 

        private readonly Dictionary<string, string> m_ResponseHeaders = new Dictionary<string, string>();
        
        internal ResponseBuilder(RequestMatchBuilder requestMatchBuilder, JsonSerializerOptions bodySerializeOptions)
        {
            m_RequestMatchBuilder = requestMatchBuilder;
            m_BodySerializeOptions = bodySerializeOptions;
        }

        public ResponseBuilder WithStatus(HttpStatusCode statusCode)
        {
            m_ResponseStatusCode = statusCode;
            return this;
        }
        
        public ResponseBuilder WithBody<T>(T body, string mediaType = "application/json", Encoding encoding = null)
        {
            m_ResponseBody = body;
            m_ResponseBodyMediaType = mediaType;
            m_ResponseBodyEncoding = encoding ?? Encoding.UTF8;
            return this;
        }
        
        public ResponseBuilder WithHeader(string name, string value)
        {
            m_ResponseHeaders.Add(name, value);
            return this;
        }

        private HttpContent BodyAsContent()
        {
            if (m_ResponseBody == null)
                return null;

            var serialisedResponseBody = JsonSerializer.Serialize(m_ResponseBody, m_BodySerializeOptions);
            
            // TODO: Non string? Multipart/FormUrl
            return new StringContent(serialisedResponseBody, m_ResponseBodyEncoding, m_ResponseBodyMediaType);
        }

        internal Response ToHttpResponse()
        {
            return new Response(m_ResponseStatusCode, BodyAsContent(), m_ResponseHeaders);
        }

        internal static Response DefaultResponse()
        {
            return new Response(HttpStatusCode.Accepted, null, new Dictionary<string, string>());
        }

        public Tracker TrackRequest()
        {
            return m_RequestMatchBuilder.TrackRequest();
        }
    }
}