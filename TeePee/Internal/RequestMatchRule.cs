using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Web;
using TeePee.Extensions;

namespace TeePee.Internal
{
    public class RequestMatchRule
    {
        private readonly TeePeeOptions m_Options;
        private readonly List<Response> m_Responses;
        private int m_CurrentResponse;

        internal Tracker? Tracker { get; }
        internal DateTimeOffset CreatedAt { get; }
        
        public string? Url { get; }
        public HttpMethod Method { get; }
        public string? RequestBody { get; }
        public string? RequestBodyMediaType { get; }
        public string? RequestBodyEncoding { get; }
        public ReadOnlyDictionary<string, string> QueryParams { get; } 
        public ReadOnlyDictionary<string, string> Headers { get; }

        internal int SpecificityLevel => (RequestBody == null ? 0 : 1) + QueryParams.Count + Headers.Count;

        internal RequestMatchRule(TeePeeOptions options, DateTimeOffset createdAt, 
                                  string? url, HttpMethod method, 
                                  string? requestBody, string? requestBodyMediaType, string? requestBodyEncoding, 
                                  IDictionary<string, string> queryParams, IDictionary<string, string> headers, 
                                  List<Response> responses, Tracker? tracker)
        {
            m_Options = options;
            CreatedAt = createdAt;

            Url = url;
            Method = method;
            RequestBody = requestBody;
            RequestBodyMediaType = requestBodyMediaType;
            RequestBodyEncoding = requestBodyEncoding;
            QueryParams = new ReadOnlyDictionary<string, string>(queryParams);
            Headers = new ReadOnlyDictionary<string, string>(headers);

            m_Responses = responses;
            Tracker = tracker;
            Tracker?.SetRequestMatchRule(this);
        }

        internal bool IsMatchingRequest(TeePeeMessageHandler.IncomingHttpCall recordedHttpCall)
        {
            var httpRequestMessage = recordedHttpCall.HttpRequestMessage;
            return IsMatchingUrl(httpRequestMessage) && 
                   Method == httpRequestMessage.Method && 
                   IsMatchingBody(recordedHttpCall.RequestBody, httpRequestMessage) && 
                   ContainsMatchingQueryParams(httpRequestMessage) &&
                   ContainsMatchingHeaders(httpRequestMessage);
        }

        private bool IsMatchingUrl(HttpRequestMessage httpRequestMessage)
        {
            if (Url == null) // Ignored
                return true;

            // If no params specified, match whole URL including QS
            if (QueryParams.Count == 0)
                return Url.IsSameUrl(httpRequestMessage.RequestUri.ToString());

            // If params specified, assume URL to match excludes QS, so match on everything else (protocol, host, port, path)
            var uriWithoutQuery = httpRequestMessage.RequestUri.RemoveQueryString();
            return Url.IsSameUrl(uriWithoutQuery);
        }

        private bool IsMatchingBody(string? requestBody, HttpRequestMessage httpRequestMessage)
        {
            if (RequestBody == null) // Ignored
                return true;

            if (requestBody == null)
                return false;
            
            if (!RequestBody.IsSameString(requestBody, m_Options.CaseSensitiveMatching))
                return false;

            return IsMatchingContentType(httpRequestMessage);
        }

        private bool IsMatchingContentType(HttpRequestMessage httpRequestMessage)
        {
            if (RequestBodyEncoding == null && RequestBodyMediaType == null) // Ignored
                return true;

            var contentType = httpRequestMessage.Content.Headers.ContentType;

            if (contentType == null)
                return false;
            
            if (RequestBodyEncoding != null && !contentType.CharSet.IsSameString(RequestBodyEncoding, m_Options.CaseSensitiveMatching))
                return false;

            if (RequestBodyMediaType != null && !contentType.MediaType.IsSameString(RequestBodyMediaType, m_Options.CaseSensitiveMatching))
                return false;

            return true;
        }

        private bool ContainsMatchingQueryParams(HttpRequestMessage httpRequestMessage)
        {
            if (QueryParams.Count == 0) // Ignored
                return true;

            var requestQueryParams = HttpUtility.ParseQueryString(httpRequestMessage.RequestUri.Query);
            return QueryParams.All(q => q.Value.IsSameString(requestQueryParams[q.Key], m_Options.CaseSensitiveMatching));
        }
        
        private bool ContainsMatchingHeaders(HttpRequestMessage httpRequestMessage)
        {
            if (Headers.Count == 0) // Ignored
                return true;

            return Headers.All(h => httpRequestMessage.Headers.Contains(h.Key) &&
                                    httpRequestMessage.Headers.GetValues(h.Key).Any(v => v.IsSameString(h.Value, m_Options.CaseSensitiveMatching)));
        }

        public string Log(int? truncateBodyLength)
        {
            return $"{Method} {Url} [Q: {QueryParams.Flat()}] [H: {Headers.Flat()}] [CE: {RequestBodyEncoding}] [CT: {RequestBodyMediaType}] [B: {RequestBody?.Trunc(truncateBodyLength)}]";
        }

        internal HttpResponseMessage ToHttpResponseMessage()
        {
            if (m_Responses.Count == 1)
                return m_Responses.Single().ToHttpResponseMessage();

            if (m_Responses.Count == m_CurrentResponse)
                throw new ArgumentOutOfRangeException($"Rule was configured with {m_Responses.Count} but was called {m_CurrentResponse + 1} times.");

            return m_Responses[m_CurrentResponse++].ToHttpResponseMessage();
        }
    }
    
    public static class RequestMatchRuleListExtensions
    {
        internal static string Log(this IEnumerable<RequestMatchRule> matchRules, TeePeeOptions options)
        {
            return string.Join("\r\n", matchRules.Select(c => $"\t{c.Log(options.TruncateBodyOutputLength)}"));
        }
    }
}