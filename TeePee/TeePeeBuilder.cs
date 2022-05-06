using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeePee
{
    public class TeePeeBuilder
    {
        private static readonly JsonSerializerOptions s_DefaultSerializeOptions = new JsonSerializerOptions
                                                                                  {
                                                                                      PropertyNamingPolicy = null,
                                                                                      Converters =
                                                                                      {
                                                                                          new JsonStringEnumConverter()
                                                                                      }
                                                                                  };

        private readonly string m_HttpClientNamedInstance;
        private readonly TeePeeMode m_Mode;
        private readonly TeePeeBuilderMode m_BuilderMode;
        private readonly JsonSerializerOptions m_BodySerializeOptions;
        private readonly List<RequestMatchBuilder> m_Requests = new List<RequestMatchBuilder>();
        
        private HttpStatusCode m_DefaultResponseStatusCode = HttpStatusCode.NotFound;
        private string m_DefaultResponseBody;

        private bool m_IsBuilt;
        
        public TeePeeBuilder() : this(default) { }

        public TeePeeBuilder(JsonSerializerOptions bodySerializeOptions = default) : this(null, default, default, bodySerializeOptions) { }

        public TeePeeBuilder(string httpClientNamedInstance = null, TeePeeMode mode = TeePeeMode.Lenient, TeePeeBuilderMode builderMode = TeePeeBuilderMode.AllowMultipleUrlRules, JsonSerializerOptions bodySerializeOptions = default)
        {
            m_HttpClientNamedInstance = httpClientNamedInstance;
            m_Mode = mode;
            m_BuilderMode = builderMode;
            m_BodySerializeOptions = bodySerializeOptions ?? s_DefaultSerializeOptions;
        }
        
        public TeePeeBuilder WithDefaultResponse(HttpStatusCode responseStatusCode, string responseBody = null)
        {
            m_DefaultResponseStatusCode = responseStatusCode;
            m_DefaultResponseBody = responseBody;
            return this;
        }
        
        /// <summary>
        /// Creates a new Request Match for the given URL and HTTP Method. Note rules around QueryStrings in URLs. 
        /// </summary>
        /// <param name="url">The URL value to match on. Absolute URLs only (Protocol, Host, Port, Path). If QueryString is included then QueryString matching can only be
        /// done using the URL for all other requests (<c>ContainingQueryParam</c> cannot be used).  It it recommended to omit QueryString from the URL
        /// here and instead use <c>ContainingQueryParam</c> - in which case incoming URLs will be stripped of all QueryString before matching the URL.
        /// </param>
        /// <param name="httpMethod">The HTTP Method to match on.</param> 
        public RequestMatchBuilder ForRequest(string url, HttpMethod httpMethod)
        {
            if (m_IsBuilt)
                throw new InvalidOperationException("Cannot add more request tracking after builder has been built.");

            var builder = new RequestMatchBuilder(this, m_BodySerializeOptions, m_BuilderMode, url, httpMethod);
            // Note: This assumes valid before adding
            m_Requests.Add(builder);
            return builder;
        }

        public TeePee Build()
        {
            m_IsBuilt = true;
            var requestMatches = m_Requests.Select(b => b.ToRequestMatch()).ToList();
            return new TeePee(m_HttpClientNamedInstance, m_Mode, requestMatches, m_DefaultResponseStatusCode, m_DefaultResponseBody);
        }

        internal bool HasMatchUrlWithQuery()
        {
            return m_Requests.Any(r => r.MatchUrlWithQuery);
        }
        
        internal bool HasMatchUrlWithQueryParams()
        {
            return m_Requests.Any(r => r.HasQueryParams);
        }

        internal bool HasMatchUrlAndMethod(string url, HttpMethod httpMethod)
        {
            return m_Requests.Any(r => r.IsSameMatchUrl(url, httpMethod));
        }
    }

    public class TeePeeBuilder<TClient> : TeePeeBuilder
    {
        public Type TypedClientType => typeof(TClient);
    }
}
