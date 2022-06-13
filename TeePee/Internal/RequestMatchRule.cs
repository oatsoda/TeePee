using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TeePee.Extensions;

namespace TeePee.Internal
{
    internal class RequestMatchRule
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
                              IDictionary<string, string> queryParams, IDictionary<string, string> headers, Response response, Tracker? tracker)
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
            m_Tracker?.SetRequestMatchRule(this);
        }

        internal bool IsMatchingRequest(HttpRequestMessage httpRequestMessage)
        {
            return IsMatchingUrl(httpRequestMessage) && 
                   Method == httpRequestMessage.Method && 
                   IsMatchingBody(httpRequestMessage) && 
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

        private bool IsMatchingBody(HttpRequestMessage httpRequestMessage)
        {
            if (RequestBody == null) // Ignored
                return true;

            if (httpRequestMessage.Content == null)
                return false;

            // TODO: Make async (or move read of Body earlier)
            var requestBody = httpRequestMessage.ReadContentAsync().GetAwaiter().GetResult();

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

        internal async Task AddCallInstance(TeePeeMessageHandler.RecordedHttpCall recordedHttpCall)
        {
            if (m_Tracker == null)
                return;

            await m_Tracker.AddCallInstance(recordedHttpCall);
        }
    }
}