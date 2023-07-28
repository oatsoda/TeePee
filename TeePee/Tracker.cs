using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using TeePee.Internal;

namespace TeePee
{
    public class Tracker
    {
        internal TeePeeOptions Options { get; }
        private readonly List<TeePeeMessageHandler.RecordedHttpCall> m_MatchedCalls = new();
        private readonly List<TeePeeMessageHandler.RecordedHttpCall> m_AllCalls = new();

        private RequestMatchRule? m_RequestMatchRule;

        public IReadOnlyList<(string? RequestBody, HttpRequestMessage Request, HttpResponseMessage Response)> MatchedCalls 
            => m_MatchedCalls.Select(c => (c.RequestBody, c.HttpRequestMessage, c.HttpResponseMessage)).ToList();

        public IReadOnlyList<(bool IsMatch, string? RequestBody, HttpRequestMessage Request, HttpResponseMessage Response)> AllCalls 
            => m_AllCalls.Select(c => (c.IsMatch, c.RequestBody, c.HttpRequestMessage, c.HttpResponseMessage)).ToList();

        internal Tracker(TeePeeOptions options)
        {
            Options = options;
        }

        internal void SetRequestMatchRule(RequestMatchRule requestMatchRule)
        {
            m_RequestMatchRule = requestMatchRule;
        }

        public void WasCalled(int? times = null)
        {
            if (m_RequestMatchRule == null)
                throw new InvalidOperationException($"Tracker was not attached to a Request Match. Ensure that you Built the {nameof(TeePeeBuilder)} instance.");

            var asExpected = times == null
                                 ? m_MatchedCalls.Any()
                                 : m_MatchedCalls.Count == times.Value;

            if (asExpected)
                return;
            
            throw new MismatchedTrackerExpectedCalls(this, m_RequestMatchRule, times, m_MatchedCalls.Count, m_AllCalls);
        }

        public void WasNotCalled() => WasCalled(0);

        internal void AddMatchedCall(TeePeeMessageHandler.RecordedHttpCall recordedHttpCall)
        {
            m_MatchedCalls.Add(recordedHttpCall);
        }
        
        internal void AddHttpCall(TeePeeMessageHandler.RecordedHttpCall recordedHttpCall)
        {
            m_AllCalls.Add(recordedHttpCall);
        }
    }

    public class MismatchedTrackerExpectedCalls : Exception
    {
        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public Tracker Tracker { get; }

        internal MismatchedTrackerExpectedCalls(Tracker tracker, RequestMatchRule matchRule, int? expectedTimes, int actualTimes, List<TeePeeMessageHandler.RecordedHttpCall> allRecordedHttpCalls) : base(CreateExceptionMessage(tracker.Options, matchRule, expectedTimes, actualTimes, allRecordedHttpCalls))
        {
            Tracker = tracker;
        }

        private static string CreateExceptionMessage(TeePeeOptions options, RequestMatchRule matchRule, int? expectedTimes, int actualTimes, List<TeePeeMessageHandler.RecordedHttpCall> allRecordedHttpCalls)
        {
            var msgTimes = expectedTimes == null ? "at least once" : $"exactly {expectedTimes.Value} times";
            var msgNotMet = expectedTimes == null ? "never called" : $"called {actualTimes} times";
            var msg = $"Expected {matchRule.Log(options.TruncateBodyOutputLength)} to be called {msgTimes} but was {msgNotMet}.\r\n\r\nTracking For:\r\n\r\n\t{matchRule.Log(options.TruncateBodyOutputLength)}\r\n\r\nAll Calls:\r\n\r\n{allRecordedHttpCalls.Log(options)}";
            return msg;
        }
    }

}