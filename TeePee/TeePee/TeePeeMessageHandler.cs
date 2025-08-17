using Microsoft.Extensions.Logging;
using TeePee.Extensions;
using TeePee.Internal;

namespace TeePee
{
    public class TeePeeMessageHandler : DelegatingHandler
    {
        private readonly TeePeeOptions m_Options;
        private readonly IReadOnlyList<RequestMatchRule> m_ConfiguredRules;
        private readonly Func<HttpResponseMessage> m_DefaultResponse;
        private readonly ILogger? m_Logger;

        internal TeePeeMessageHandler(TeePeeOptions options, IReadOnlyList<RequestMatchRule> requestMatchRules, Func<HttpResponseMessage> defaultResponse, ILogger? logger)
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
            foreach (var ruleWithTracker in m_ConfiguredRules.Where(r => r.Tracker != null))
                ruleWithTracker.Tracker!.AddHttpCall(recordedHttpCall);

            if (!recordedHttpCall.IsMatch && m_Options.Mode == TeePeeMode.Strict)
                throw new NotSupportedException($"Unmatched Http request: {recordedHttpCall.Log(m_Options)} [Response: {(int)recordedHttpCall.HttpResponseMessage.StatusCode} {recordedHttpCall.HttpResponseMessage.StatusCode}] [{m_ConfiguredRules.Count} rules configured]");

            if (m_Logger == null)
                return;

            if (recordedHttpCall.IsMatch)
            {
                m_Logger.LogMatchedRequest(
                    recordedHttpCall.Log(m_Options),
                    (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                    recordedHttpCall.HttpResponseMessage.StatusCode);
                return;
            }

            if (m_Options.ShowFullDetailsOnMatchFailure)
            {
                m_Logger.LogUnmatchedRequestWithFullDetails(
                    recordedHttpCall.Log(m_Options),
                    (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                    recordedHttpCall.HttpResponseMessage.StatusCode,
                    m_ConfiguredRules.Log(m_Options));
                return;
            }

            m_Logger.LogUnmatchedRequest(
                recordedHttpCall.Log(m_Options),
                (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                recordedHttpCall.HttpResponseMessage.StatusCode,
                m_ConfiguredRules.Count);
        }

        internal record IncomingHttpCall(HttpRequestMessage HttpRequestMessage, string? RequestBody);

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

    internal static partial class RequestLoggingExtensions
    {
        [LoggerMessage(
            Message = "Matched Http request: {request} [Response: {responseCode} {responseCodeDescription}]",
            Level = LogLevel.Information)]
        internal static partial void LogMatchedRequest(
            this ILogger logger,
            string request,
            int responseCode,
            System.Net.HttpStatusCode responseCodeDescription);

        [LoggerMessage(
            Message = "Unmatched Http request: {request} [Response: {responseCode} {responseCodeDescription}] [{numberOfRulesConfigured} rules configured]",
            Level = LogLevel.Warning)]
        internal static partial void LogUnmatchedRequest(
            this ILogger logger,
            string request,
            int responseCode,
            System.Net.HttpStatusCode responseCodeDescription,
            int numberOfRulesConfigured);

        [LoggerMessage(
            Message = "Unmatched Http request: {request} [Response: {responseCode} {responseCodeDescription}]\r\n\r\nConfigured Rules:\r\n\r\n{rulesConfigured}",
            Level = LogLevel.Warning)]
        internal static partial void LogUnmatchedRequestWithFullDetails(
            this ILogger logger,
            string request,
            int responseCode,
            System.Net.HttpStatusCode responseCodeDescription,
            string rulesConfigured);
    }
}