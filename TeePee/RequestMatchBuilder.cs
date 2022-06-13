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
        private readonly JsonSerializerOptions m_BodySerializeOptions;
        private ResponseBuilder m_ResponseBuilder;
        private Tracker m_Tracker;
        
        private string Url { get; }
        private HttpMethod Method { get; }
        private object RequestBody { get; set; }
        private Encoding RequestBodyEncoding { get; set; }
        private string RequestBodyMediaType { get; set; }
        
        private Dictionary<string, string> QueryParams { get; } = new Dictionary<string, string>();
        private Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        
        internal bool MatchUrlWithQuery { get; private set; }
        internal bool HasQueryParams => QueryParams.Any();
        
        internal bool IsSameMatchUrl(string url, HttpMethod httpMethod) => Method == httpMethod && 
                                                                           Url.IsSameString(url);

        internal RequestMatchBuilder(TeePeeBuilder parentTrackingBuilder, JsonSerializerOptions bodySerializeOptions, TeePeeBuilderMode builderMode, string url, HttpMethod httpMethod)
        {
            m_ParentTrackingBuilder = parentTrackingBuilder;
            m_BodySerializeOptions = bodySerializeOptions;

            Url = WithUrl(url);
            Method = httpMethod;

            if (builderMode == TeePeeBuilderMode.RequireUniqueUrlRules && m_ParentTrackingBuilder.HasMatchUrlAndMethod(url, httpMethod))
                throw new ArgumentException($"There is already a request match for {httpMethod} '{url}'");
        }

        private string WithUrl(string url)
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

        public RequestMatchBuilder WithBody<T>(T body, string mediaType = "application/json", Encoding encoding = null)
        {
            if (body == null)
                throw new ArgumentNullException();

            if (RequestBody != null)
                throw new InvalidOperationException("The matching Body has already been added to this request match.");

            RequestBody = body;
            RequestBodyEncoding = encoding ?? Encoding.UTF8;
            RequestBodyMediaType = mediaType;
            return this;
        }

        public RequestMatchBuilder ContainingQueryParam(string name, string value)
        {
            if (MatchUrlWithQuery)
                throw new InvalidOperationException($"You cannot use ContainingQueryParam as Url has already been configured to match with a QueryString. '{Url}'");
            
            if (m_ParentTrackingBuilder.HasMatchUrlWithQuery())
                throw new InvalidOperationException("You cannot use ContainingQueryParam as request matches already exist with QueryString matching.");

            QueryParams.Add(name, value);
            return this;
        }

        public RequestMatchBuilder ContainingHeader(string name, string value)
        {
            Headers.Add(name, value);
            return this;
        }
        
        public ResponseBuilder Responds()
        {
            m_ResponseBuilder = new ResponseBuilder(this, m_BodySerializeOptions);
            return m_ResponseBuilder;
        }

        internal RequestMatchRule ToRequestMatch()
        {
            var serialisedRequestBody = RequestBody == null ? null : JsonSerializer.Serialize(RequestBody); // TODO: JSON Serialiser options
            var response = m_ResponseBuilder == null 
                               ? ResponseBuilder.DefaultResponse()
                               : m_ResponseBuilder.ToHttpResponse();
            return new RequestMatchRule(Url, Method, serialisedRequestBody, RequestBodyMediaType, RequestBodyEncoding, QueryParams, Headers, response, m_Tracker);
        }

        public Tracker TrackRequest()
        {
            return m_Tracker ??= new Tracker();
        }
    }
}