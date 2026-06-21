using Microsoft.Extensions.Logging;
using TeePee.Extensions;
using TeePee.Internal;

namespace TeePee
{
    public class TeePeeMessageHandler : DelegatingHandler
    {
        private readonly TeePeeBuilder m_AttachedBuilder;

        // TODO: Had to make Public for TeePee.Refit
        public TeePeeMessageHandler(TeePeeBuilder builder)
        {
            m_AttachedBuilder = builder;
        }

        private async Task<TeePee> GetCongfiguration() => await m_AttachedBuilder.GetCurrentRules();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var teePee = await GetCongfiguration();

            var requestBody = await request.ReadContentAsync();
            var incomingHttpCall = new IncomingHttpCall(request, requestBody);

            var match = teePee.MatchRules.FirstOrDefault(m => m.IsMatchingRequest(incomingHttpCall));

            Func<HttpResponseMessage> defaultResponse = () => new(teePee.UnmatchedStatusCode)
            {
                Content = teePee.UnmatchedBody == null
                    ? null
                    : new StringContent(teePee.UnmatchedBody)
            };

            var recordedHttpCall = new RecordedHttpCall(incomingHttpCall, match, defaultResponse);
            RecordRequest(teePee, recordedHttpCall);

            return recordedHttpCall.HttpResponseMessage;
        }

        private void RecordRequest(TeePee teePee, RecordedHttpCall recordedHttpCall)
        {
            foreach (var ruleWithTracker in teePee.MatchRules.Where(r => r.Tracker != null))
                ruleWithTracker.Tracker!.TrackingState.AddHttpCall(recordedHttpCall);

            if (!recordedHttpCall.IsMatch && m_AttachedBuilder.Options.Mode == TeePeeMode.Strict)
                throw new NotSupportedException($"Unmatched Http request: {recordedHttpCall.Log(m_AttachedBuilder.Options)} [Response: {(int)recordedHttpCall.HttpResponseMessage.StatusCode} {recordedHttpCall.HttpResponseMessage.StatusCode}] [{teePee.MatchRules.Count} rules configured]");

            if (m_AttachedBuilder.Options.Logger == null)
                return;

            if (recordedHttpCall.IsMatch)
            {
                m_AttachedBuilder.Options.Logger.LogMatchedRequest(
                    recordedHttpCall.Log(m_AttachedBuilder.Options),
                    (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                    recordedHttpCall.HttpResponseMessage.StatusCode);
                return;
            }

            if (m_AttachedBuilder.Options.ShowFullDetailsOnMatchFailure)
            {
                m_AttachedBuilder.Options.Logger.LogUnmatchedRequestWithFullDetails(
                    recordedHttpCall.Log(m_AttachedBuilder.Options),
                    (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                    recordedHttpCall.HttpResponseMessage.StatusCode,
                    teePee.MatchRules.Log(m_AttachedBuilder.Options));
                return;
            }

            m_AttachedBuilder.Options.Logger.LogUnmatchedRequest(
                recordedHttpCall.Log(m_AttachedBuilder.Options),
                (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                recordedHttpCall.HttpResponseMessage.StatusCode,
                teePee.MatchRules.Count);
        }

        //internal void Reset(
        //    IReadOnlyList<RequestMatchRule> requestMatchRules,
        //    Func<HttpResponseMessage> defaultResponse)
        //{
        //    m_ConfiguredRules = requestMatchRules;
        //    m_DefaultResponse = defaultResponse;

        //    foreach (var ruleWithTracker in m_ConfiguredRules.Where(r => r.Tracker != null))
        //        ruleWithTracker.Tracker!.SetTrackingState(new HttpTrackingState());
        //}

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
                    MatchRule.Tracker?.TrackingState.AddMatchedCall(this);
                }
            }

            public string Log(TeePeeOptions options)
            {
                return $"{HttpRequestMessage.Method} {HttpRequestMessage.RequestUri} [H: {HttpRequestMessage.Headers.ToDictionary(h => h.Key, h => h.Value).Flat()}] [CE: {HttpRequestMessage.Content?.Headers?.ContentType?.CharSet}] [CT: {HttpRequestMessage.Content?.Headers?.ContentType?.MediaType}] [B: {RequestBody?.Trunc(options.TruncateBodyOutputLength)}] [Matched: {MatchRule != null}]";
            }
        }
    }

    internal class HttpTrackingState
    {
        private readonly List<TeePeeMessageHandler.RecordedHttpCall> m_MatchedCalls = [];
        private readonly List<TeePeeMessageHandler.RecordedHttpCall> m_AllCalls = [];

        internal IReadOnlyList<TeePeeMessageHandler.RecordedHttpCall> MatchedCalls => m_MatchedCalls;
        internal IReadOnlyList<TeePeeMessageHandler.RecordedHttpCall> AllCalls => m_AllCalls;

        internal void AddMatchedCall(TeePeeMessageHandler.RecordedHttpCall recordedHttpCall)
        {
            m_MatchedCalls.Add(recordedHttpCall);
        }

        internal void AddHttpCall(TeePeeMessageHandler.RecordedHttpCall recordedHttpCall)
        {
            m_AllCalls.Add(recordedHttpCall);
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