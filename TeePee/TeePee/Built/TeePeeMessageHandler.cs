using Microsoft.Extensions.Logging;
using TeePee.Extensions;

namespace TeePee.Built
{
    internal class TeePeeMessageHandler : DelegatingHandler
    {
        private readonly TeePeeBuilder m_AttachedBuilder;

        internal TeePeeMessageHandler(TeePeeBuilder builder)
        {
            m_AttachedBuilder = builder;
        }

        private async Task<TeePeeSeeded> GetCongfiguration() => await m_AttachedBuilder.GetCurrentRules();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var teePeeSeeded = await GetCongfiguration();

            var requestBody = await request.ReadContentAsync();
            var incomingHttpCall = new IncomingHttpCall(request, requestBody);

            var checkAllMatches = teePeeSeeded.Options.ShowAllMatchesOnMatch && teePeeSeeded.Options.LoggerFactory != null;

            List<RequestMatchRule> matches = [];
            List<RequestMatchRule> rulesToTrack = [];
            foreach (var matchRule in teePeeSeeded.MatchRulesOrdered)
            {
                if ((checkAllMatches || matches.Count == 0) && matchRule.IsMatchingRequest(incomingHttpCall))
                {
                    matches.Add(matchRule);
                }

                if (matchRule.Tracker != null)
                    rulesToTrack.Add(matchRule);
            }

            // This will also trigger Tracker tracking of matched call
            var recordedHttpCall = new RecordedHttpCall(incomingHttpCall, matches.FirstOrDefault(), teePeeSeeded.DefaultResponse);

            // All Trackers record all calls
            foreach (var ruleWithTracker in rulesToTrack)
                ruleWithTracker.Tracker!.TrackingState.AddHttpCall(recordedHttpCall);

            if (!recordedHttpCall.IsMatch && teePeeSeeded.Options.Mode == TeePeeMode.Strict)
                throw new NotSupportedException($"Unmatched Http request: {recordedHttpCall.Log(teePeeSeeded.Options)} [Response: {(int)recordedHttpCall.HttpResponseMessage.StatusCode} {recordedHttpCall.HttpResponseMessage.StatusCode}] [{teePeeSeeded.MatchRulesOrdered.Count} rules configured]");

            LogRequest(teePeeSeeded, recordedHttpCall, matches);

            return recordedHttpCall.HttpResponseMessage;
        }

        private static void LogRequest(TeePeeSeeded teePeeSeeded, RecordedHttpCall recordedHttpCall, IReadOnlyList<RequestMatchRule> allMatches)
        {
            if (teePeeSeeded.Options.LoggerFactory == null)
                return;

            var logger = teePeeSeeded.Options.LoggerFactory();

            if (recordedHttpCall.IsMatch)
            {
                if (allMatches.Count > 1)
                {
                    logger.LogMatchedRequestWithMultipleMatches(
                        recordedHttpCall.Log(teePeeSeeded.Options),
                        (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                        recordedHttpCall.HttpResponseMessage.StatusCode,
                        allMatches.Log(teePeeSeeded.Options));
                    return;
                }

                logger.LogMatchedRequest(
                    recordedHttpCall.Log(teePeeSeeded.Options),
                    (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                    recordedHttpCall.HttpResponseMessage.StatusCode);
                return;
            }

            if (teePeeSeeded.Options.ShowFullDetailsOnMatchFailure)
            {
                logger.LogUnmatchedRequestWithFullDetails(
                    recordedHttpCall.Log(teePeeSeeded.Options),
                    (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                    recordedHttpCall.HttpResponseMessage.StatusCode,
                    teePeeSeeded.MatchRulesOrdered.Log(teePeeSeeded.Options));
                return;
            }

            logger.LogUnmatchedRequest(
                recordedHttpCall.Log(teePeeSeeded.Options),
                (int)recordedHttpCall.HttpResponseMessage.StatusCode,
                recordedHttpCall.HttpResponseMessage.StatusCode,
                teePeeSeeded.MatchRulesOrdered.Count);
        }

        internal record IncomingHttpCall(HttpRequestMessage HttpRequestMessage, string? RequestBody);

        internal class RecordedHttpCall
        {
            public HttpRequestMessage HttpRequestMessage { get; }
            public string? RequestBody { get; }

            public HttpResponseMessage HttpResponseMessage { get; }
            public RequestMatchRule? MatchRule { get; }

            public bool IsMatch => MatchRule != null;

            internal RecordedHttpCall(IncomingHttpCall incomingHttpCall, RequestMatchRule? matchedRule, Func<HttpResponseMessage> nonMatchedDefaultResponseFactory)
            {
                HttpRequestMessage = incomingHttpCall.HttpRequestMessage;
                RequestBody = incomingHttpCall.RequestBody;

                if (matchedRule == null)
                {
                    HttpResponseMessage = nonMatchedDefaultResponseFactory();
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

            public string Log(ITeePeeOptions options)
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
            Message = "Matched Http request: {request} [Response: {responseCode} {responseCodeDescription}]\r\n\r\nMultiple Matching Rules (in priority order, first was used):\r\n\r\n{matchingRules}",
            Level = LogLevel.Information)]
        internal static partial void LogMatchedRequestWithMultipleMatches(
            this ILogger logger,
            string request,
            int responseCode,
            System.Net.HttpStatusCode responseCodeDescription,
            string matchingRules);

        [LoggerMessage(
            Message = "Request body could not be deserialised to {ruleType} for partial body match rule, so rule not matched: {rule}",
            Level = LogLevel.Information)]
        internal static partial void LogBodyContainingRuleDeserialisationFailure(
            this ILogger logger,
            string ruleType,
            string rule,
            Exception exception);

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