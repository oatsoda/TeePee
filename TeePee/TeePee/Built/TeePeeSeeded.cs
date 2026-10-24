using System.Net;

namespace TeePee.Built
{
    public class TeePeeSeeded
    {
        internal ITeePeeOptions Options { get; }
        internal IReadOnlyList<RequestMatchRule> MatchRulesOrdered { get; }
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
            UnmatchedStatusCode = unmatchedStatusCode;
            UnmatchedBody = unmatchedBody;
        }
    }
}
