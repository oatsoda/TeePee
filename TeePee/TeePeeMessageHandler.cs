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
        
        internal TeePeeMessageHandler(TeePeeOptions options, List<RequestMatchRule> requestMatchRules, Func<HttpResponseMessage> defaultResponse, ILogger? logger)
        {
            m_Options = options;
            m_ConfiguredRules = requestMatchRules;
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
            
            return recordedHttpCall.HttpResponseMessage;
        }

        private void RecordRequest(RecordedHttpCall recordedHttpCall)
        {
            m_ConfiguredRules.Where(r => r.Tracker != null).ToList().ForEach(r => r.Tracker!.AddHttpCall(recordedHttpCall));

            if (!recordedHttpCall.IsMatch && m_Options.Mode == TeePeeMode.Strict)
                throw new NotSupportedException($"Unmatched Http request: {recordedHttpCall.Log(m_Options)} [Response: {(int)recordedHttpCall.HttpResponseMessage.StatusCode} {recordedHttpCall.HttpResponseMessage.StatusCode}] [{m_ConfiguredRules.Count} rules configured]");

            if (m_Logger == null)
                return;

            if (recordedHttpCall.IsMatch)
            {
                m_Logger.LogInformation("Matched Http request: {request} [Response: {responseCode} {response}]",
                                        recordedHttpCall.Log(m_Options),
                                        (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                                        recordedHttpCall.HttpResponseMessage.StatusCode);
                return;
            }

            if (!m_Options.ShowFullDetailsOnMatchFailure)
            {
                m_Logger.LogWarning("Unmatched Http request: {request} [Response: {responseCode} {response}] [{num} rules configured]",
                                    recordedHttpCall.Log(m_Options),
                                    (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                                    recordedHttpCall.HttpResponseMessage.StatusCode,
                                    m_ConfiguredRules.Count);
                return;
            }

            m_Logger.LogWarning("Unmatched Http request: {request} [Response: {responseCode} {response}]\r\n\r\nConfigured Rules:\r\n\r\n{rules}", 
                                recordedHttpCall.Log(m_Options), 
                                (int)recordedHttpCall.HttpResponseMessage.StatusCode, 
                                recordedHttpCall.HttpResponseMessage.StatusCode, 
                                m_ConfiguredRules.Log(m_Options));
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
                    MatchRule.Tracker?.AddMatchedCall(this);
                }
            }
            
            public string Log(TeePeeOptions options)
            {
                return $"{HttpRequestMessage.Method} {HttpRequestMessage.RequestUri} [H: {HttpRequestMessage.Headers.ToDictionary(h => h.Key, h => h.Value).Flat()}] [CE: {HttpRequestMessage.Content?.Headers?.ContentType?.CharSet}] [CT: {HttpRequestMessage.Content?.Headers?.ContentType?.MediaType}] [B: {RequestBody?.Trunc(options.TruncateBodyOutputLength)}] [Matched: {MatchRule != null}]";
            }
        }
    }
    
    public static class RecordedHttpCallListExtensions
    {
        internal static string Log(this IEnumerable<TeePeeMessageHandler.RecordedHttpCall> recordedHttpCalls, TeePeeOptions options)
        {
            return string.Join("\r\n", recordedHttpCalls.Select(c => $"\t{c.Log(options)}"));
        }
    }
}