using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using TeePee.Internal;

namespace TeePee
{
    public class ResponseBuilder
    {
        private static readonly string s_DefaultResponseBodyMediaType = "application/json";
        private static readonly Encoding s_DefaultResponseBodyEncoding = Encoding.UTF8; 
        
        private readonly RequestMatchBuilder m_RequestMatchBuilder;
        private readonly TeePeeOptions m_Options;

        private HttpStatusCode m_ResponseStatusCode = HttpStatusCode.NoContent;

        private object? m_ResponseBody;
        private string m_ResponseBodyMediaType = s_DefaultResponseBodyMediaType;
        private Encoding m_ResponseBodyEncoding = s_DefaultResponseBodyEncoding; 

        private readonly Dictionary<string, string> m_ResponseHeaders = new Dictionary<string, string>();
        
        internal ResponseBuilder(RequestMatchBuilder requestMatchBuilder, TeePeeOptions options)
        {
            m_RequestMatchBuilder = requestMatchBuilder;
            m_Options = options;
        }

        public ResponseBuilder WithStatus(HttpStatusCode statusCode)
        {
            m_ResponseStatusCode = statusCode;
            return this;
        }
        
        public ResponseBuilder WithBody<T>(T body, string? mediaType = null, Encoding? encoding = null)
        {
            m_ResponseBody = body;
            if (mediaType != null)
                m_ResponseBodyMediaType = mediaType;
            if (encoding != null)
                m_ResponseBodyEncoding = encoding;
            return this;
        }
        
        public ResponseBuilder WithHeader(string name, string value)
        {
            m_ResponseHeaders.Add(name, value);
            return this;
        }

        internal Response ToHttpResponse()
        {
            return new Response(m_ResponseStatusCode, m_Options, m_ResponseBody, m_ResponseBodyMediaType, m_ResponseBodyEncoding, m_ResponseHeaders);
        }

        internal static Response DefaultResponse(TeePeeOptions options)
        {
            return new Response(HttpStatusCode.Accepted, options, null, s_DefaultResponseBodyMediaType, s_DefaultResponseBodyEncoding, new Dictionary<string, string>());
        }

        public Tracker TrackRequest()
        {
            return m_RequestMatchBuilder.TrackRequest();
        }
    }
}
