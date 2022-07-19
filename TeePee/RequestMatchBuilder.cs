using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using TeePee.Extensions;
using TeePee.Internal;

namespace TeePee
{
    public class RequestMatchBuilder
    {
        private readonly TeePeeBuilder m_ParentTrackingBuilder;
        private readonly TeePeeOptions m_Options;

        private ResponseBuilder? m_ResponseBuilder;
        private Tracker? m_Tracker;

        private readonly DateTimeOffset m_CreatedAt = DateTimeOffset.UtcNow;

        private string Url { get; }
        private HttpMethod Method { get; }
        private object? RequestBody { get; set; }
        private HttpContent? RequestBodyContent { get; set; }
        private string? RequestBodyMediaType { get; set; }
        private string? RequestBodyEncoding { get; set; }
        
        private Dictionary<string, string> QueryParams { get; } = new();
        private Dictionary<string, string> Headers { get; } = new();
        
        internal bool MatchUrlWithQuery { get; private set; }
        internal bool HasQueryParams => QueryParams.Any();
        
        internal bool IsSameMatchUrl(string url, HttpMethod httpMethod) => Method == httpMethod && 
                                                                           Url.IsSameUrl(url);

        internal RequestMatchBuilder(TeePeeBuilder parentTrackingBuilder, TeePeeOptions options, string url, HttpMethod httpMethod)
        {
            m_ParentTrackingBuilder = parentTrackingBuilder;
            m_Options = options;

            Url = ThatHasUrl(url);
            Method = httpMethod;

            if (m_Options.BuilderMode == TeePeeBuilderMode.RequireUniqueUrlRules && m_ParentTrackingBuilder.HasMatchUrlAndMethod(url, httpMethod))
                throw new ArgumentException($"There is already a request match for {httpMethod} '{url}'");
        }

        private string ThatHasUrl(string url)
        {
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);

            if (!uri.IsAbsoluteUri)
                throw new ArgumentException($"Url must be an absolute URI rather than relative. '{url}'", nameof(url));

            if (uri.Query.Length > 0)
            {
                MatchUrlWithQuery = true;

                if (QueryParams.Any())
                    throw new ArgumentException($"Url must not contain QueryString as request has already been configured to match containing a QueryParam. {QueryParams.Keys.Flat()}");

                if (m_ParentTrackingBuilder.HasMatchUrlWithQueryParams())
                    throw new ArgumentException($"Url must not contain QueryString as request matches already exist with <c>ContainingQueryParam</c>. '{url}'", nameof(url));
            }

            return url;
        }

        [Obsolete("Use ThatHasBody instead.")]
        public RequestMatchBuilder WithBody<T>(T body, string? mediaType = "application/json", Encoding? encoding = null) => ThatHasBody(body, mediaType, encoding);

        /// <summary>
        /// REQUEST Match this request with the given JSON Body. MediaType and Encoding default to application/json / UTF8 respectively.
        /// </summary>
        public RequestMatchBuilder ThatHasBody<T>(T body, string? mediaType = "application/json", Encoding? encoding = null)
        {
            if (body == null)
                throw new ArgumentNullException();
            
            if (RequestBody != null || RequestBodyContent != null)
                throw new InvalidOperationException("The matching Body has already been added to this request match.");

            RequestBody = body;
            RequestBodyMediaType = mediaType;
            RequestBodyEncoding = encoding?.WebName ?? Encoding.UTF8.WebName; // Json Body defaults to UTF8, instead of ignore.
            return this;
        }
        
        /// <summary>
        /// REQUEST Match this request with the given HttpContent Body. Use <c>ThatHasJsonBody</c> for JSON Body content.
        /// </summary>
        public RequestMatchBuilder ThatHasHttpContentBody(HttpContent body)
        {
            if (body == null)
                throw new ArgumentNullException();
            
            if (RequestBody != null || RequestBodyContent != null)
                throw new InvalidOperationException("The matching Body has already been added to this request match.");

            RequestBodyContent = body;
            RequestBodyMediaType = body.Headers.ContentType?.MediaType;
            RequestBodyEncoding = body.Headers.ContentType?.CharSet;
            return this;
        }
        
        /// <summary>
        /// REQUEST Match this request with the given Querystring Parameter in the URL.
        /// </summary>
        public RequestMatchBuilder ThatContainsQueryParam(string name, string value)
        {
            if (MatchUrlWithQuery)
                throw new InvalidOperationException($"You cannot use ContainingQueryParam as Url has already been configured to match with a QueryString. '{Url}'");
            
            if (m_ParentTrackingBuilder.HasMatchUrlWithQuery())
                throw new InvalidOperationException("You cannot use ContainingQueryParam as request matches already exist with QueryString matching.");

            QueryParams.Add(name, value);
            return this;
        }
        
        /// <summary>
        /// REQUEST Match this request with the given Header Parameter in the request.
        /// </summary>
        public RequestMatchBuilder ThatContainsHeader(string name, string value)
        {
            Headers.Add(name, value);
            return this;
        }
        
        /// <summary>
        /// Define that the Request will respond.
        /// </summary>
        public ResponseBuilder Responds()
        {
            m_ResponseBuilder = new ResponseBuilder(this, m_Options);
            return m_ResponseBuilder;
        }

        internal RequestMatchRule ToRequestMatchRule()
        {
            var serialisedRequestBody = RequestBodyContent != null 
                                            ? RequestBodyContent.ReadContentAsync().GetAwaiter().GetResult()
                                            : RequestBody == null
                                                ? null 
                                                : JsonSerializer.Serialize(RequestBody, m_Options.RequestBodySerializerOptions);

            var response = m_ResponseBuilder == null 
                               ? ResponseBuilder.DefaultResponse(m_Options)
                               : m_ResponseBuilder.ToHttpResponse();

            return new RequestMatchRule(m_Options, m_CreatedAt, Url, Method, serialisedRequestBody, RequestBodyMediaType, RequestBodyEncoding, QueryParams, Headers, response, m_Tracker);
        }
        
        public Tracker TrackRequest()
        {
            return m_Tracker ??= new Tracker(m_Options);
        }
    }
}