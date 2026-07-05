using System.Net;

namespace TeePee.Built
{
    public class TeePeeSeeded
    {
        internal ITeePeeOptions Options { get; }
        internal IReadOnlyList<RequestMatchRule> MatchRules { get; }
        internal HttpStatusCode UnmatchedStatusCode { get; }
        internal string? UnmatchedBody { get; }

        internal TeePeeSeeded(
            ITeePeeOptions options,
            IReadOnlyList<RequestMatchRule> matchRules,
            HttpStatusCode unmatchedStatusCode,
            string? unmatchedBody)
        {
            Options = options;
            MatchRules = matchRules;
            UnmatchedStatusCode = unmatchedStatusCode;
            UnmatchedBody = unmatchedBody;
        }
    }
}
