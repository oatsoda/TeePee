using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using TeePee.Internal;

namespace TeePee
{
    public class Tracker
    {
        private readonly List<TeePeeMessageHandler.RecordedHttpCall> m_MatchedCalls = new List<TeePeeMessageHandler.RecordedHttpCall>();
        private readonly List<TeePeeMessageHandler.RecordedHttpCall> m_AllCalls = new List<TeePeeMessageHandler.RecordedHttpCall>();

        private RequestMatchRule? m_RequestMatchRule;

        public IReadOnlyList<(string? RequestBody, HttpRequestMessage Request, HttpResponseMessage Response)> MatchedCalls 
            => m_MatchedCalls.Select(c => (c.RequestBody, c.HttpRequestMessage, c.HttpResponseMessage)).ToList();

        public IReadOnlyList<(bool IsMatch, string? RequestBody, HttpRequestMessage Request, HttpResponseMessage Response)> AllCalls 
            => m_AllCalls.Select(c => (c.IsMatch, c.RequestBody, c.HttpRequestMessage, c.HttpResponseMessage)).ToList();

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

        internal MismatchedTrackerExpectedCalls(Tracker tracker, RequestMatchRule matchRule, int? expectedTimes, int actualTimes, List<TeePeeMessageHandler.RecordedHttpCall> allRecordedHttpCalls) : base(CreateExceptionMessage(matchRule, expectedTimes, actualTimes, allRecordedHttpCalls))
        {
            Tracker = tracker;
        }

        private static string CreateExceptionMessage(RequestMatchRule matchRule, int? expectedTimes, int actualTimes, List<TeePeeMessageHandler.RecordedHttpCall> allRecordedHttpCalls)
        {
            var msgTimes = expectedTimes == null ? "at least once" : $"exactly {expectedTimes.Value} times";
            var msgNotMet = expectedTimes == null ? "never called" : $"call {actualTimes} times";
            var msg = $"Expected match on {matchRule} {msgTimes} but was {msgNotMet}.\r\n\r\nTracking For:\r\n\r\n\t{matchRule}\r\n\r\nAll Calls:\r\n\r\n{LogCallsMade(allRecordedHttpCalls)}";
            return msg;
        }

        private static string LogCallsMade(List<TeePeeMessageHandler.RecordedHttpCall> allRecordedHttpCalls)
        {
            return string.Join("\r\n", allRecordedHttpCalls.Select(c => $"\t{c}"));
        }
    }
}