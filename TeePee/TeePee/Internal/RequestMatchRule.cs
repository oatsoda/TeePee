using System.Collections.ObjectModel;
using System.Text.Json;
using System.Web;
using TeePee.Extensions;

namespace TeePee.Internal
{
    internal class RequestMatchRule
    {
        private readonly TeePeeOptions m_Options;
        private readonly List<Response> m_Responses;
        private int m_CurrentResponse;

        internal Tracker? Tracker { get; }
        internal DateTimeOffset CreatedAt { get; }

        public string Url { get; }
        public HttpMethod Method { get; }

        public string? RequestBody { get; }
        public RequestBodyContainingRule? RequestBodyContainingRule { get; }
        public string? RequestBodyMediaType { get; }
        public string? RequestBodyEncoding { get; }

        public ReadOnlyDictionary<string, string> QueryParams { get; }
        public ReadOnlyDictionary<string, string> Headers { get; }

        private bool BodyMatchIsSpecified => RequestBody != null || RequestBodyContainingRule != null;
        internal int SpecificityLevel => (BodyMatchIsSpecified ? 1 : 0) + QueryParams.Count + Headers.Count;

        internal RequestMatchRule(TeePeeOptions options, DateTimeOffset createdAt,
                                  string url, HttpMethod method,
                                  bool isHttpContentBodyMatch, string? requestBody, RequestBodyContainingRule? requestBodyContainingRule, string? requestBodyMediaType, string? requestBodyEncoding,
                                  IDictionary<string, string> queryParams, IDictionary<string, string> headers,
                                  List<Response> responses, Tracker? tracker)
        {
            m_Options = options;
            CreatedAt = createdAt;

            Url = url;
            Method = method;

            RequestBody = requestBody;
            RequestBodyContainingRule = requestBodyContainingRule;
            RequestBodyMediaType = requestBodyMediaType;
            RequestBodyEncoding = requestBodyEncoding;

            if (!isHttpContentBodyMatch && BodyMatchIsSpecified)
            {
                if (RequestBodyMediaType == null)
                    throw new ArgumentNullException(nameof(requestBodyMediaType), "Body MediaType must be specified if matching a JSON body.");

                if (RequestBodyEncoding == null)
                    throw new ArgumentNullException(nameof(requestBodyEncoding), "Body Encoding must be specified if matching a JSON body.");
            }

            QueryParams = new(queryParams);
            Headers = new(headers);

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
            ArgumentNullException.ThrowIfNull(httpRequestMessage.RequestUri);

            // If no params specified, match whole URL including QS
            if (QueryParams.Count == 0)
                return Url.IsSameUrl(httpRequestMessage.RequestUri.ToString());

            // If params specified, assume URL to match excludes QS, so match on everything else (protocol, host, port, path)
            var uriWithoutQuery = httpRequestMessage.RequestUri.RemoveQueryString();
            return Url.IsSameUrl(uriWithoutQuery);
        }

        private bool IsMatchingBody(string? requestBody, HttpRequestMessage httpRequestMessage)
        {
            if (!BodyMatchIsSpecified) // Ignored
                return true;

            if (requestBody == null)
                return false;

            if (RequestBody != null && !RequestBody.IsSameString(requestBody, m_Options.CaseSensitiveMatching))
                return false;

            if (RequestBodyContainingRule != null && !RequestBodyContainingRule.Rule(JsonSerializer.Deserialize(requestBody, RequestBodyContainingRule.RuleType, m_Options.RequestBodySerializerOptions)!))
                return false;

            return IsMatchingContentType(httpRequestMessage);
        }

        private bool IsMatchingContentType(HttpRequestMessage httpRequestMessage)
        {
            if (RequestBodyEncoding == null && RequestBodyMediaType == null) // Ignored
                return true;

            ArgumentNullException.ThrowIfNull(httpRequestMessage.Content);

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

            ArgumentNullException.ThrowIfNull(httpRequestMessage.RequestUri);

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
            var body = RequestBodyContainingRule != null ? "<Partial Containing Rule>" : RequestBody?.Trunc(truncateBodyLength);
            return $"{Method} {Url} [Q: {QueryParams.Flat()}] [H: {Headers.Flat()}] [CE: {RequestBodyEncoding}] [CT: {RequestBodyMediaType}] [B: {body}]";
        }

        internal HttpResponseMessage ToHttpResponseMessage()
        {
            if (m_Responses.Count == m_CurrentResponse)
                m_CurrentResponse--;

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
