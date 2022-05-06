using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TeePee.Extensions;
using TeePee.Internal;

namespace TeePee
{
    public class TeePeeMessageHandler : DelegatingHandler
    {
        private readonly TeePeeMode m_Mode;
        private readonly List<RequestMatch> m_Matches;
        private readonly Func<HttpResponseMessage> m_DefaultResponse;

        internal readonly List<HttpRecord> HttpRecords = new List<HttpRecord>();

        internal TeePeeMessageHandler(TeePeeMode mode, List<RequestMatch> matches, Func<HttpResponseMessage> defaultResponse)
        {
            m_Mode = mode;
            m_Matches = matches;
            m_DefaultResponse = defaultResponse;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var match = m_Matches.OrderByDescending(m => m.SpecificityLevel).FirstOrDefault(m => m.IsMatchingRequest(request));

            if (match == null && m_Mode == TeePeeMode.Strict)
                throw new NotSupportedException($"An HTTP request was made which did not match any of the TeePee rules. [{request.Method} {request.RequestUri}]");

            HttpRecord httpRecord;
            if (match == null)
            {
                httpRecord = new HttpRecord(request, m_DefaultResponse);

                HttpRecords.Add(httpRecord);
                return Task.FromResult(httpRecord.HttpResponseMessage);
            }
            
            httpRecord = new HttpRecord(request, match);
            
            HttpRecords.Add(httpRecord);
            return Task.FromResult(httpRecord.HttpResponseMessage);
        }

        public override string ToString()
        {
            var calls = string.Join("\r\n", HttpRecords.Select(r => $"\t{r}"));
            return $"Calls made:\r\n\r\n{calls}";
        }

        // Do we need this? Maybe for verifying etc. or user debugging
        internal class HttpRecord
        {
            public HttpRequestMessage HttpRequestMessage { get; }
            public RequestMatch Match { get; }
            public HttpResponseMessage HttpResponseMessage { get; }

            public HttpRecord(HttpRequestMessage httpRequestMessage, Func<HttpResponseMessage> defaultResponse)
            {
                HttpRequestMessage = httpRequestMessage;
                Match = null;
                HttpResponseMessage = defaultResponse();
                HttpResponseMessage.RequestMessage = httpRequestMessage;
            }

            public HttpRecord(HttpRequestMessage httpRequestMessage, RequestMatch match)
            {
                HttpRequestMessage = httpRequestMessage;
                Match = match;
                HttpResponseMessage = match.ToHttpResponseMessage();
                HttpResponseMessage.RequestMessage = httpRequestMessage;
                Match.AddCallInstance(this);
            }

            public override string ToString()
            {
                var body = HttpRequestMessage.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                return $"{HttpRequestMessage.Method} {HttpRequestMessage.RequestUri} [H: {HttpRequestMessage.Headers.Select(h => h.Key).Flat()}] [B: {body?.Trunc()}] [Matched: {Match != null}";
            }
        }
    }
}