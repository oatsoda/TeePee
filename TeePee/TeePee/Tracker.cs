using TeePee.Internal;

namespace TeePee
{
    public class Tracker
    {
        internal TeePeeOptions Options { get; }

        private RequestMatchRule? m_RequestMatchRule;
        private HttpTrackingState? m_TrackingState;

        internal HttpTrackingState TrackingState => m_TrackingState
            ?? throw new InvalidOperationException($"Tracker was not attached to a Tracking State");

        public IReadOnlyList<MatchedCall> MatchedCalls
            => TrackingState.MatchedCalls.Select(c => new MatchedCall(c.RequestBody, c.HttpRequestMessage, c.HttpResponseMessage)).ToList()
                 ?? throw new InvalidOperationException($"Tracker was not attached to a Tracking State");

        public IReadOnlyList<Call> AllCalls
            => TrackingState.AllCalls.Select(c => new Call(c.IsMatch, c.RequestBody, c.HttpRequestMessage, c.HttpResponseMessage)).ToList()
                ?? throw new InvalidOperationException($"Tracker was not attached to a Tracking State");

        internal Tracker(TeePeeOptions options)
        {
            Options = options;
        }

        internal void SetRequestMatchRule(RequestMatchRule requestMatchRule)
        {
            m_RequestMatchRule = requestMatchRule;
        }

        internal void SetTrackingState(HttpTrackingState trackingState)
        {
            m_TrackingState = trackingState;
        }

        public void WasCalled(int? times = null)
        {
            if (m_RequestMatchRule == null)
                throw new InvalidOperationException($"Tracker was not attached to a Request Match. Ensure that you built the {nameof(TeePeeBuilder)} instance.");

            if (m_TrackingState == null)
                throw new InvalidOperationException($"Tracker was not attached to a Tracking State");

            var asExpected = times == null
                                 ? m_TrackingState.MatchedCalls.Count > 0
                                 : m_TrackingState.MatchedCalls.Count == times.Value;

            if (asExpected)
                return;

            throw new MismatchedTrackerExpectedCalls(this, m_RequestMatchRule, times, m_TrackingState.MatchedCalls.Count, m_TrackingState.AllCalls);
        }

        public void WasNotCalled() => WasCalled(0);

    }

    public record MatchedCall(string? RequestBody, HttpRequestMessage Request, HttpResponseMessage Response);
    public record Call(bool IsMatch, string? RequestBody, HttpRequestMessage Request, HttpResponseMessage Response);


    public class MismatchedTrackerExpectedCalls : Exception
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public Tracker Tracker { get; }

        internal MismatchedTrackerExpectedCalls(Tracker tracker, RequestMatchRule matchRule, int? expectedTimes, int actualTimes, IReadOnlyList<TeePeeMessageHandler.RecordedHttpCall> allRecordedHttpCalls)
            : base(CreateExceptionMessage(tracker.Options, matchRule, expectedTimes, actualTimes, allRecordedHttpCalls))
        {
            Tracker = tracker;
        }

        private static string CreateExceptionMessage(TeePeeOptions options, RequestMatchRule matchRule, int? expectedTimes, int actualTimes, IReadOnlyList<TeePeeMessageHandler.RecordedHttpCall> allRecordedHttpCalls)
        {
            var msgTimes = expectedTimes == null ? "at least once" : $"exactly {expectedTimes.Value} times";
            var msgNotMet = expectedTimes == null ? "never called" : $"called {actualTimes} times";
            var msg = $"""
Expected {matchRule.Log(options.TruncateBodyOutputLength)} to be called {msgTimes} but was {msgNotMet}.
                
Tracking For:
                
{"\t"}{matchRule.Log(options.TruncateBodyOutputLength)}
                
All Calls:
                
{string.Join("\r\n", allRecordedHttpCalls.Select(c => $"\t{c.Log(options)}"))}
""";
            return msg;
        }
    }

}