﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TeePee
{
    public class TeePeeBuilder
    {
        private readonly string? m_HttpClientNamedInstance;
        private readonly TeePeeOptions m_Options = new TeePeeOptions();

        private readonly List<RequestMatchBuilder> m_Requests = new List<RequestMatchBuilder>();
        
        private HttpStatusCode m_DefaultResponseStatusCode = HttpStatusCode.NotFound;
        private string? m_DefaultResponseBody;

        private bool m_IsBuilt;
        
        public TeePeeBuilder() : this(null, null) { }

        public TeePeeBuilder(JsonSerializerOptions responseBodySerializeOptions) : this(opt => opt.ResponseBodySerializerOptions = responseBodySerializeOptions) { }
        public TeePeeBuilder(string httpClientNamedInstance) : this(null, httpClientNamedInstance) { }
        
        public TeePeeBuilder(Action<TeePeeOptions>? setOptions = null, string? httpClientNamedInstance = null)
        {
            if (setOptions != null)
                setOptions(m_Options);

            m_HttpClientNamedInstance = httpClientNamedInstance;
        }
        
        public TeePeeBuilder WithDefaultResponse(HttpStatusCode responseStatusCode, string? responseBody = null)
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

            var builder = new RequestMatchBuilder(this, m_Options, url, httpMethod);
            // Note: This assumes valid before adding
            m_Requests.Add(builder);
            return builder;
        }

        public TeePee Build(ILogger<TeePee>? logger = null)
        {
            m_IsBuilt = true;
            return new TeePee(m_HttpClientNamedInstance, m_Options, m_Requests, m_DefaultResponseStatusCode, m_DefaultResponseBody, logger);
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
