using System.Net;

namespace TeePee.Built
{
    public class TeePeeSeeded
    {
        internal IReadOnlyList<RequestMatchRule> MatchRules { get; }
        internal HttpStatusCode UnmatchedStatusCode { get; }
        internal string? UnmatchedBody { get; }

        internal TeePeeSeeded(IReadOnlyList<RequestMatchRule> matchRules,
                        HttpStatusCode unmatchedStatusCode,
                        string? unmatchedBody)
        {
            MatchRules = matchRules;
            UnmatchedStatusCode = unmatchedStatusCode;
            UnmatchedBody = unmatchedBody;
        }
    }
}
