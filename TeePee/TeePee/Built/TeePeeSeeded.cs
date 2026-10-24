using System.Net;

namespace TeePee.Built
{
    public class TeePeeSeeded
    {
        internal ITeePeeOptions Options { get; }
        internal IReadOnlyList<RequestMatchRule> MatchRulesOrdered { get; }
        internal IReadOnlyList<Tracker> Trackers { get; }
        internal HttpStatusCode UnmatchedStatusCode { get; }
        internal string? UnmatchedBody { get; }

        internal TeePeeSeeded(
            ITeePeeOptions options,
            IReadOnlyList<RequestMatchRule> matchRulesOrdered,
            HttpStatusCode unmatchedStatusCode,
            string? unmatchedBody)
        {
            Options = options;
            MatchRulesOrdered = matchRulesOrdered;
            Trackers = matchRulesOrdered.Where(r => r.Tracker != null).Select(r => r.Tracker!).ToList();
            UnmatchedStatusCode = unmatchedStatusCode;
            UnmatchedBody = unmatchedBody;
        }

        internal HttpResponseMessage DefaultResponse()
            => new(UnmatchedStatusCode)
            {
                Content = UnmatchedBody == null
                    ? null
                    : new StringContent(UnmatchedBody)
            };
    }
}
