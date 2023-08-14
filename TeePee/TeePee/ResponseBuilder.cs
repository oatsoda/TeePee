using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using TeePee.Internal;

namespace TeePee
{
    public class ResponseBuilder
    {
        private readonly RequestMatchBuilder m_RequestMatchBuilder;
        private readonly TeePeeOptions m_Options;

        private HttpStatusCode m_ResponseStatusCode = HttpStatusCode.NoContent;

        private object? m_ResponseBody;
        private HttpContent? m_ResponseBodyContent;
        private string? m_ResponseBodyMediaType;
        private string? m_ResponseBodyEncoding; 

        private readonly Dictionary<string, string> m_ResponseHeaders = new();

        internal ResponseBuilder? NextResponse;

        internal ResponseBuilder(RequestMatchBuilder requestMatchBuilder, TeePeeOptions options)
        {
            m_RequestMatchBuilder = requestMatchBuilder;
            m_Options = options;
        }

        /// <summary>
        /// RESPONSE Respond with the given Http Status.
        /// </summary>
        public ResponseBuilder WithStatus(HttpStatusCode statusCode)
        {
            m_ResponseStatusCode = statusCode;
            return this;
        }
        
        /// <summary>
        /// RESPONSE Respond with the given JSON Body. MediaType and Encoding default to application/json / UTF8 respectively.
        /// </summary>
        public ResponseBuilder WithBody<T>(T body, string? mediaType = "application/json", Encoding? encoding  = null)
        {
            if (m_ResponseBodyContent != null)
                throw new InvalidOperationException("The response Body has already been set from HttpContent.");

            m_ResponseBody = body;
            m_ResponseBodyMediaType = mediaType;
            m_ResponseBodyEncoding = encoding?.WebName ?? Encoding.UTF8.WebName; // Json Body defaults to UTF8.
            return this;
        }
        
        /// <summary>
        /// RESPONSE Respond with the given HttpContent Body. Use <c>WithBody</c> for JSON Body content.
        /// </summary>
        public ResponseBuilder WithHttpContentBody(HttpContent body)
        {
            if (m_ResponseBody != null)
                throw new InvalidOperationException("The response Body has already been set from Json Body.");

            m_ResponseBodyContent = body;
            // ContentType and Encoding should be set on the HttpContent as required
            return this;
        }
        
        /// <summary>
        /// RESPONSE Respond with the given Header Parameter on the response.
        /// </summary>
        public ResponseBuilder WithHeader(string name, string value)
        {
            m_ResponseHeaders.Add(name, value);
            return this;
        }
        
        /// <summary>
        /// Define that the Request will then respond with a different response.
        /// </summary>
        public ResponseBuilder ThenResponds()
        {
            NextResponse = new(m_RequestMatchBuilder, m_Options);
            return NextResponse;
        }

        #region Create Tracker

        public Tracker TrackRequest()
        {
            return m_RequestMatchBuilder.TrackRequest();
        }
        
        #endregion

        #region Internal: Create Response

        internal Response ToHttpResponse()
        {
            return new(m_ResponseStatusCode, m_Options, m_ResponseBody, m_ResponseBodyContent, m_ResponseBodyMediaType, m_ResponseBodyEncoding, m_ResponseHeaders);
        }

        internal static Response DefaultResponse(TeePeeOptions options)
        {
            return new(HttpStatusCode.Accepted, options, null, null, null, null, new Dictionary<string, string>());
        }

        #endregion


    }
}
