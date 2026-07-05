using System.Net;
using System.Text.Json;
using TeePee.Built;

namespace TeePee
{
    public class TeePeeBuilder
    {
        internal TeePeeOptions Options { get; } = new();

        private readonly List<RequestMatchBuilder> m_Requests = new();
        private static readonly HttpStatusCode m_DefaultDefaultResponseStatusCode = HttpStatusCode.NotFound;

        private HttpStatusCode m_DefaultResponseStatusCode = m_DefaultDefaultResponseStatusCode;
        private string? m_DefaultResponseBody;

        private bool m_IsBuilt;
        private TeePeeSeeded? m_AttachedTeePee; // TeePee is attached once on first build, but Builder can be reset and built many times.

        public TeePeeBuilder() : this(null, null) { }

        public TeePeeBuilder(JsonSerializerOptions responseBodySerializeOptions) : this(opt => opt.ResponseBodySerializerOptions = responseBodySerializeOptions) { }

        public TeePeeBuilder(Action<TeePeeOptions>? setOptions = null, string? httpClientNamedInstance = null)
        {
            setOptions?.Invoke(Options);
        }

        public TeePeeBuilder WithDefaultResponse(HttpStatusCode responseStatusCode, string? responseBody = null)
        {
            m_DefaultResponseStatusCode = responseStatusCode;
            m_DefaultResponseBody = responseBody;
            return this;
        }

        /// <summary>
        /// Creates a new Request Match for the given URL and HTTP Method. Note rules around QueryStrings in URLs. 
        /// </summary>
        /// <param name="url">The URL value to match on. Absolute URLs only (Protocol, Host, Port, Path). If QueryString is included then QueryString matching can only be
        /// done using the URL for all other requests (<c>ContainingQueryParam</c> cannot be used).  It it recommended to omit QueryString from the URL
        /// here and instead use <c>ContainingQueryParam</c> - in which case incoming URLs will be stripped of all QueryString before matching the URL.
        /// </param>
        /// <param name="httpMethod">The HTTP Method to match on.</param> 
        public RequestMatchBuilder ForRequest(string url, HttpMethod httpMethod)
        {
            if (m_IsBuilt)
                throw new InvalidOperationException("Cannot add more request tracking after builder has been used.");

            var builder = new RequestMatchBuilder(this, Options, url, httpMethod);
            m_Requests.Add(builder); // Note: This assumes valid before adding
            return builder;
        }

        internal bool HasMatchUrlWithQuery()
        {
            return m_Requests.Any(r => r.MatchUrlWithQuery);
        }

        internal bool HasMatchUrlWithQueryParams()
        {
            return m_Requests.Any(r => r.HasQueryParams);
        }

        internal bool HasMatchUrlAndMethod(string url, HttpMethod httpMethod)
        {
            return m_Requests.Any(r => r.IsSameMatchUrl(url, httpMethod));
        }

        private async Task<TeePeeSeeded> Build()
        {
            m_IsBuilt = true;
            var requestMatchRules = new List<RequestMatchRule>(m_Requests.Count);
            foreach (var request in m_Requests)
            {
                var requestMatchRule = await request.ToRequestMatchRule();
                requestMatchRules.Add(requestMatchRule);
            }

            var requestMatchRulesOrdered = requestMatchRules
                                          .OrderByDescending(m => m.SpecificityLevel)
                                          .ThenByDescending(m => m.CreatedAt)
                                          .ToList();

            m_AttachedTeePee = new(requestMatchRulesOrdered, m_DefaultResponseStatusCode, m_DefaultResponseBody);
            return m_AttachedTeePee;
        }

        internal async Task<TeePeeSeeded> GetCurrentRules()
        {
            if (m_IsBuilt)
            {
                return m_AttachedTeePee!;
            }

            return await Build();
        }

        public void Reset()
        {
            m_DefaultResponseStatusCode = m_DefaultDefaultResponseStatusCode;
            m_DefaultResponseBody = null;
            m_Requests.Clear();
            m_IsBuilt = false;
        }
    }
}
