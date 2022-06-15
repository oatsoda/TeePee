using Microsoft.Extensions.Logging;
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
        private readonly TeePeeOptions m_Options;
        private readonly List<RequestMatchRule> m_ConfiguredRules;
        private readonly Func<HttpResponseMessage> m_DefaultResponse;
        private readonly ILogger? m_Logger;
        
        internal TeePeeMessageHandler(TeePeeOptions options, IEnumerable<RequestMatchBuilder> requestMatchBuilders, Func<HttpResponseMessage> defaultResponse, ILogger? logger)
        {
            m_Options = options;
            m_ConfiguredRules = requestMatchBuilders
                               .Select(b => b.ToRequestMatchRule())
                               .OrderByDescending(m => m.SpecificityLevel)
                               .ThenByDescending(m => m.CreatedAt)
                               .ToList();
            m_DefaultResponse = defaultResponse;
            m_Logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestBody = await request.ReadContentAsync();
            var incomingHttpCall = new IncomingHttpCall(request, requestBody);

            var match = m_ConfiguredRules.FirstOrDefault(m => m.IsMatchingRequest(incomingHttpCall));
            
            var recordedHttpCall = new RecordedHttpCall(incomingHttpCall, match, m_DefaultResponse);
            RecordRequest(recordedHttpCall);

            if (match == null && m_Options.Mode == TeePeeMode.Strict)
                throw new NotSupportedException($"An HTTP request was made which did not match any of the TeePee rules. [{request.Method} {request.RequestUri}]");

            return recordedHttpCall.HttpResponseMessage;
        }

        private void RecordRequest(RecordedHttpCall recordedHttpCall)
        {
            m_ConfiguredRules.ForEach(r => r.AddHttpCall(recordedHttpCall));
            
            if (m_Logger == null)
                return;
            
            if (recordedHttpCall.IsMatch)
                m_Logger.LogInformation("Matched Http request: {request} [Response: {responseCode} {response}]", recordedHttpCall.ToString(), (int)recordedHttpCall.HttpResponseMessage.StatusCode, recordedHttpCall.HttpResponseMessage.StatusCode);
            else
                m_Logger.LogWarning("Unmatched Http request: {request} [Response: {responseCode} {response}] [{num} rules configured]", recordedHttpCall.ToString(), (int)recordedHttpCall.HttpResponseMessage.StatusCode, recordedHttpCall.HttpResponseMessage.StatusCode, m_ConfiguredRules.Count);
        }
        
        internal class IncomingHttpCall
        {
            public HttpRequestMessage HttpRequestMessage { get; }
            public string? RequestBody { get; }
            
            public IncomingHttpCall(HttpRequestMessage httpRequestMessage, string? requestBody)
            {
                HttpRequestMessage = httpRequestMessage;
                RequestBody = requestBody;
            }
        }
        
        internal class RecordedHttpCall
        {
            public HttpRequestMessage HttpRequestMessage { get; }
            public string? RequestBody { get; }
            
            public HttpResponseMessage HttpResponseMessage { get; }
            public RequestMatchRule? MatchRule { get; }

            public bool IsMatch => MatchRule != null;

            internal RecordedHttpCall(IncomingHttpCall incomingHttpCall, RequestMatchRule? matchedRule, Func<HttpResponseMessage> nonMatchedDefaultResponse)
            {
                HttpRequestMessage = incomingHttpCall.HttpRequestMessage;
                RequestBody = incomingHttpCall.RequestBody;

                if (matchedRule == null)
                {
                    HttpResponseMessage = nonMatchedDefaultResponse();
                    HttpResponseMessage.RequestMessage = HttpRequestMessage;
                }
                else
                {
                    HttpResponseMessage = matchedRule.ToHttpResponseMessage();
                    HttpResponseMessage.RequestMessage = HttpRequestMessage;
                    
                    MatchRule = matchedRule;
                    MatchRule.AddMatchedCall(this);
                }
            }
            
            public override string ToString()
            {
                return $"{HttpRequestMessage.Method} {HttpRequestMessage.RequestUri} [H: {HttpRequestMessage.Headers.ToDictionary(h => h.Key, h => h.Value).Flat()}] [B: {RequestBody?.Trunc()}] [Matched: {MatchRule != null}]";
            }
        }
    }
}