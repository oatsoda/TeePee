using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TeePee.Extensions;
using TeePee.Internal;

namespace TeePee
{
    public class Tracker
    {
        private readonly List<(TeePeeMessageHandler.RecordedHttpCall Record, string RequestBody)> m_Calls = new List<(TeePeeMessageHandler.RecordedHttpCall, string)>();

        private RequestMatchRule m_RequestMatchRule;

        public IEnumerable<(string RequestBody, HttpResponseMessage Response)> Calls => m_Calls.Select(c => (c.RequestBody, c.Record.HttpResponseMessage));

        internal void SetRequestMatchRule(RequestMatchRule requestMatchRule)
        {
            m_RequestMatchRule = requestMatchRule;
        }

        public void WasCalled(int? times = null)
        {
            if (m_RequestMatchRule == null)
                throw new InvalidOperationException($"Tracker was not attached to a Request Match. Ensure that you Built the {nameof(TeePeeBuilder)} instance.");

            var asExpected = times == null
                                 ? m_Calls.Any()
                                 : m_Calls.Count == times.Value;

            if (asExpected)
                return;

            var msgTimes = times == null ? "at least once" : $"exactly {times.Value} times";
            var msgNotMet = times == null ? "never called" : $"call {m_Calls.Count} times";
            var msg = $"Expected match on {m_RequestMatchRule} {msgTimes} but was {msgNotMet}.";
            throw new IncorrectExpectedRequests(msg);
        }

        public void WasNotCalled() => WasCalled(0);

        internal async Task AddCallInstance(TeePeeMessageHandler.RecordedHttpCall recordedHttpCall)
        {
            var requestBody = await recordedHttpCall.HttpRequestMessage.ReadContentAsync();
            m_Calls.Add((recordedHttpCall, requestBody));
        }
    }

    public class IncorrectExpectedRequests : Exception
    {
        public IncorrectExpectedRequests(string message) : base(message)
        {
            
        }
    }
}