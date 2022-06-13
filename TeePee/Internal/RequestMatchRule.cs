using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using TeePee.Extensions;

namespace TeePee.Internal
{
    public class RequestMatchRule
    {
        private readonly Response m_Response;
        private readonly Tracker? m_Tracker;

        public string? Url { get; }
        public HttpMethod Method { get; }
        public string? RequestBody { get; }
        public string RequestBodyMediaType { get; }
        public Encoding RequestBodyEncoding { get; }
        public ReadOnlyDictionary<string, string> QueryParams { get; } 
        public ReadOnlyDictionary<string, string> Headers { get; }

        internal int SpecificityLevel => (RequestBody == null ? 0 : 1) + QueryParams.Count + Headers.Count;

        internal RequestMatchRule(string? url, HttpMethod method, string? requestBody, string requestBodyMediaType, Encoding requestBodyEncoding, 
                              IDictionary<string, string> queryParams, IDictionary<string, string> headers, Response response, Tracker? tracker, 
                              List<TeePeeMessageHandler.RecordedHttpCall> recordedHttpCalls)
        {
            Url = url;
            Method = method;
            RequestBody = requestBody;
            RequestBodyMediaType = requestBodyMediaType;
            RequestBodyEncoding = requestBodyEncoding;
            QueryParams = new ReadOnlyDictionary<string, string>(queryParams);
            Headers = new ReadOnlyDictionary<string, string>(headers);

            m_Response = response;
            m_Tracker = tracker;
            m_Tracker?.SetRequestMatchRule(this, recordedHttpCalls);
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
                return Url.IsSameString(httpRequestMessage.RequestUri.ToString());

            // If params specified, assume URL to match excludes QS, so match on everything else (protocol, host, port, path)
            var uriWithoutQuery = httpRequestMessage.RequestUri.RemoveQueryString();
            return Url.IsSameString(uriWithoutQuery);
        }

        private bool IsMatchingBody(string? requestBody, HttpRequestMessage httpRequestMessage)
        {
            if (RequestBody == null) // Ignored
                return true;

            if (requestBody == null)
                return false;
            
            if (!RequestBody.IsSameString(requestBody))
                return false;

            var contentType = httpRequestMessage.Content.Headers.ContentType;
            return contentType.CharSet.IsSameString(RequestBodyEncoding.WebName) &&
                   contentType.MediaType.IsSameString(RequestBodyMediaType);
        }

        private bool ContainsMatchingQueryParams(HttpRequestMessage httpRequestMessage)
        {
            if (QueryParams.Count == 0) // Ignored
                return true;

            var requestQueryParams = HttpUtility.ParseQueryString(httpRequestMessage.RequestUri.Query);
            return QueryParams.All(q => q.Value.IsSameString(requestQueryParams[q.Key]));
        }
        
        private bool ContainsMatchingHeaders(HttpRequestMessage httpRequestMessage)
        {
            if (Headers.Count == 0) // Ignored
                return true;

            return Headers.All(h => httpRequestMessage.Headers.Contains(h.Key) &&
                                    httpRequestMessage.Headers.GetValues(h.Key).Any(v => v.IsSameString(h.Value)));
        }

        public override string ToString()
        {
            return $"{Method} {Url} [Q: {QueryParams.Flat()}] [H: {Headers.Flat()}] [B: {RequestBody?.Trunc()}]";
        }
        
        internal HttpResponseMessage ToHttpResponseMessage() => m_Response.ToHttpResponseMessage();

        internal void AddCallInstance(TeePeeMessageHandler.RecordedHttpCall recordedHttpCall)
        {
            m_Tracker?.AddCallInstance(recordedHttpCall);
        }
    }
}