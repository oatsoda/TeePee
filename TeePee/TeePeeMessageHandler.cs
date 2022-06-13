using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TeePee.Extensions;
using TeePee.Internal;

namespace TeePee
{
    public class TeePeeMessageHandler : DelegatingHandler
    {
        private readonly TeePeeMode m_Mode;
        private readonly List<RequestMatchRule> m_ConfiguredRules;
        private readonly Func<HttpResponseMessage> m_DefaultResponse;
        private readonly ILogger? m_Logger;

        private readonly List<RecordedHttpCall> m_HttpRequestsMade = new List<RecordedHttpCall>();

        internal TeePeeMessageHandler(TeePeeMode mode, List<RequestMatchRule> configuredRules, Func<HttpResponseMessage> defaultResponse, ILogger? logger)
        {
            m_Mode = mode;
            m_ConfiguredRules = configuredRules;
            m_DefaultResponse = defaultResponse;
            m_Logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var match = m_ConfiguredRules.OrderByDescending(m => m.SpecificityLevel).FirstOrDefault(m => m.IsMatchingRequest(request));

            if (match == null && m_Mode == TeePeeMode.Strict)
                throw new NotSupportedException($"An HTTP request was made which did not match any of the TeePee rules. [{request.Method} {request.RequestUri}]");

            var recordedHttpCall = match == null 
                                 ? await RecordedHttpCall.NotMatched(request, m_DefaultResponse) 
                                 : await RecordedHttpCall.Matched(request, match);

            m_HttpRequestsMade.Add(recordedHttpCall);
            await LogRequest(recordedHttpCall);
            return recordedHttpCall.HttpResponseMessage;
        }

        private async Task LogRequest(RecordedHttpCall recordedHttpCall)
        {
            if (m_Logger == null)
                return;

            var httpRecordLog = await recordedHttpCall.LogOutput();

            if (recordedHttpCall.IsMatch)
                m_Logger.LogInformation("Matched Http request: {request} [Response: {responseCode} {response}]", httpRecordLog, (int)recordedHttpCall.HttpResponseMessage.StatusCode, recordedHttpCall.HttpResponseMessage.StatusCode);
            else
                m_Logger.LogWarning("Unmatched Http request: {request} [Response: {responseCode} {response}] [{num} rules configured]", httpRecordLog, (int)recordedHttpCall.HttpResponseMessage.StatusCode, recordedHttpCall.HttpResponseMessage.StatusCode, m_ConfiguredRules.Count);
        }

        //public override string ToString()
        //{
        //    var calls = string.Join("\r\n", m_HttpRequestsMade.Select(r => $"\t{r}"));
        //    return $"Calls made:\r\n\r\n{calls}";
        //}
        
        internal class RecordedHttpCall
        {
            public HttpRequestMessage HttpRequestMessage { get; private set; }
            public RequestMatchRule? MatchRule { get; private set; }
            public HttpResponseMessage HttpResponseMessage { get; private set; }

            public bool IsMatch => MatchRule != null;

            private RecordedHttpCall(HttpRequestMessage httpRequestMessage, HttpResponseMessage httpResponseMessage)
            {
                HttpRequestMessage = httpRequestMessage;
                HttpResponseMessage = httpResponseMessage;
            }

            public static Task<RecordedHttpCall> NotMatched(HttpRequestMessage httpRequestMessage, Func<HttpResponseMessage> defaultResponse)
            {
                var response = defaultResponse();
                response.RequestMessage = httpRequestMessage;
                return Task.FromResult(new RecordedHttpCall(httpRequestMessage, response));
            }

            public static async Task<RecordedHttpCall> Matched(HttpRequestMessage httpRequestMessage, RequestMatchRule matchRule)
            {
                var response = matchRule.ToHttpResponseMessage();
                response.RequestMessage = httpRequestMessage;
                var recordedHttpCall = new RecordedHttpCall(httpRequestMessage, response)
                       {
                           MatchRule = matchRule
                       };
                await matchRule.AddCallInstance(recordedHttpCall);
                return recordedHttpCall;
            }

            public async Task<string> LogOutput()
            {
                var body = await HttpRequestMessage.ReadContentAsync();
                return $"{HttpRequestMessage.Method} {HttpRequestMessage.RequestUri} [H: {HttpRequestMessage.Headers.ToDictionary(h => h.Key, h => h.Value).Flat()}] [B: {body?.Trunc()}] [Matched: {MatchRule != null}]";
            }
        }
    }
}