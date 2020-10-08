using System;
using System.Collections.Generic;
using System.Linq;
using TeePee.Internal;

namespace TeePee
{
    public class Tracker
    {
        private readonly List<TeePeeMessageHandler.HttpRecord> m_Calls = new List<TeePeeMessageHandler.HttpRecord>();

        private RequestMatch m_RequestMatch;

        internal void SetRequestMatch(RequestMatch requestMatch)
        {
            if (m_RequestMatch != null)
                throw new InvalidOperationException("Request match has already been set.");

            m_RequestMatch = requestMatch;
        }

        public void WasCalled(int? times = null)
        {
            if (m_RequestMatch == null)
                throw new InvalidOperationException($"Tracker was not attached to a Request Match. Ensure that you Built the {nameof(TeePeeBuilder)} instance.");

            var asExpected = times == null
                                 ? m_Calls.Any()
                                 : m_Calls.Count == times.Value;

            if (asExpected)
                return;

            var msgTimes = times == null ? "at least once" : $"exactly {times.Value} times";
            var msgNotMet = times == null ? "never called" : $"call {m_Calls.Count} times";
            var msg = $"Expected match on {m_RequestMatch} {msgTimes} but was {msgNotMet}.";
            throw new InvalidOperationException(msg);
        }

        public void WasNotCalled() => WasCalled(0);

        internal void AddCallInstance(TeePeeMessageHandler.HttpRecord httpRecord)
        {
            m_Calls.Add(httpRecord);
        }
    }
}