using System.Text;
using System.Text.Json;
using TeePee.Extensions;
using TeePee.Internal;

namespace TeePee
{
    public class RequestMatchBuilder
    {
        private readonly TeePeeBuilder m_ParentTrackingBuilder;
        private readonly TeePeeOptions m_Options;

        private ResponseBuilder? m_ResponseBuilder;
        private Tracker? m_Tracker;

        private readonly DateTimeOffset m_CreatedAt = DateTimeOffset.UtcNow;

        private string Url { get; }
        private HttpMethod Method { get; }

        // Body match is either
        // a) An object instance which will be serialised when the TeePeeBuilder is built (i.e. before the SUT is executed).
        private object? m_RequestBody;
        // b) A raw HttpContent object which will have its content read when the TeePeeBuilder is built (i.e. before the SUT is executed).
        private HttpContent? m_RequestBodyContent;
        // c) A delegate rule expecting the type and rules about the values it contains, which will only be evaluated at execution time of the SUT.
        private RequestBodyContainingRule? m_RequestBodyContainingRule;

        private string? m_RequestBodyMediaType;
        private string? m_RequestBodyEncoding;

        private readonly Dictionary<string, string> m_QueryParams = new(4);
        private readonly Dictionary<string, string> m_Headers = new(4);

        internal bool MatchUrlWithQuery { get; private set; }
        internal bool HasQueryParams => m_QueryParams.Count > 0;

        internal bool IsSameMatchUrl(string url, HttpMethod httpMethod) => Method == httpMethod &&
                                                                           Url.IsSameUrl(url);

        internal RequestMatchBuilder(TeePeeBuilder parentTrackingBuilder, TeePeeOptions options, string url, HttpMethod httpMethod)
        {
            m_ParentTrackingBuilder = parentTrackingBuilder;
            m_Options = options;

            var uri = new Uri(url, UriKind.RelativeOrAbsolute);

            if (!uri.IsAbsoluteUri)
                throw new ArgumentException($"Url must be an absolute URI rather than relative. '{url}'", nameof(url));

            if (m_Options.BuilderMode == TeePeeBuilderMode.RequireUniqueUrlRules && m_ParentTrackingBuilder.HasMatchUrlAndMethod(url, httpMethod))
                throw new ArgumentException($"There is already a request match for {httpMethod} '{url}'");

            MatchUrlWithQuery = uri.Query.Length > 0;

            if (MatchUrlWithQuery && m_ParentTrackingBuilder.HasMatchUrlWithQueryParams())
                throw new ArgumentException($"Url must not contain QueryString as request matches already exist using ThatContainsQueryParam. '{url}'", nameof(url));

            Url = url;
            Method = httpMethod;
        }

        /// <summary>
        /// REQUEST Match this request with the given JSON Body. MediaType and Encoding default to application/json / UTF8 respectively.
        /// </summary>
        public RequestMatchBuilder ThatHasBody<T>(T body, string? mediaType = "application/json", Encoding? encoding = null) where T : notnull
        {
            if (body == null)
                throw new ArgumentNullException();

            if (m_RequestBody != null || m_RequestBodyContent != null || m_RequestBodyContainingRule != null)
                throw new InvalidOperationException("The matching Body has already been added to this request match.");

            m_RequestBody = body;
            m_RequestBodyMediaType = mediaType;
            m_RequestBodyEncoding = encoding?.WebName ?? Encoding.UTF8.WebName; // Json Body defaults to UTF8, instead of ignore.
            return this;
        }

        /// <summary>
        /// REQUEST Match this request with the given HttpContent Body. Use <c>ThatHasBody</c> for JSON Body content.
        /// </summary>
        public RequestMatchBuilder ThatHasHttpContentBody(HttpContent body)
        {
            if (body == null) // Force check for null even though nullable not allowing.
                throw new ArgumentNullException();

            if (m_RequestBody != null || m_RequestBodyContent != null || m_RequestBodyContainingRule != null)
                throw new InvalidOperationException("The matching Body has already been added to this request match.");

            m_RequestBodyContent = body;
            m_RequestBodyMediaType = body.Headers.ContentType?.MediaType;
            m_RequestBodyEncoding = body.Headers.ContentType?.CharSet;
            return this;
        }

        /// <summary>
        /// REQUEST Match this request with the given rule to apply to the expected JSON type specified by the Type parameter. WARNING: Any reference
        /// types you use within the rule delegate will be evaluated at the point of request (as opposed to at TeePee Build() time) so you must be careful
        /// not to use the same ref types to seed your test data and this matching rule. MediaType and Encoding default to application/json / UTF8 respectively.
        /// </summary>
        public RequestMatchBuilder ThatHasBodyContaining<T>(Func<T, bool> bodyMatchRule, string? mediaType = "application/json", Encoding? encoding = null) where T : class
        {
            if (bodyMatchRule == null)
                throw new ArgumentNullException();

            if (m_RequestBody != null || m_RequestBodyContent != null || m_RequestBodyContainingRule != null)
                throw new InvalidOperationException("The matching Body has already been added to this request match.");

            m_RequestBodyContainingRule = new(typeof(T), o => bodyMatchRule((T)o));
            m_RequestBodyMediaType = mediaType;
            m_RequestBodyEncoding = encoding?.WebName ?? Encoding.UTF8.WebName; // Json Body defaults to UTF8, instead of ignore.
            return this;
        }

        /// <summary>
        /// REQUEST Match this request with the given Querystring Parameter in the URL.
        /// </summary>
        public RequestMatchBuilder ThatContainsQueryParam(string name, string value)
        {
            if (MatchUrlWithQuery)
                throw new InvalidOperationException($"You cannot use ContainingQueryParam as Url has already been configured to match with a QueryString. '{Url}'");

            if (m_ParentTrackingBuilder.HasMatchUrlWithQuery())
                throw new InvalidOperationException("You cannot use ContainingQueryParam as request matches already exist with QueryString matching.");

            m_QueryParams.Add(name, value);
            return this;
        }

        /// <summary>
        /// REQUEST Match this request with the given Header Parameter in the request.
        /// </summary>
        public RequestMatchBuilder ThatContainsHeader(string name, string value)
        {
            m_Headers.Add(name, value);
            return this;
        }

        /// <summary>
        /// Define that the Request will respond.
        /// </summary>
        public ResponseBuilder Responds()
        {
            if (m_ResponseBuilder != null)
                throw new InvalidOperationException("You can only call Responds once per rule.");

            m_ResponseBuilder = new(this, m_Options);
            return m_ResponseBuilder;
        }

        #region Create Tracker

        public Tracker TrackRequest()
        {
            return m_Tracker ??= new(m_Options);
        }

        #endregion

        #region Internal: Build Rule into Responses

        internal async Task<RequestMatchRule> ToRequestMatchRule()
        {
            var serialisedRequestBody = await SerialiseExpectedRequestMatchBody();
            var responses = CreateResponses();
            return new(m_Options, m_CreatedAt,
                       Url, Method,
                       m_RequestBodyContent != null, serialisedRequestBody, m_RequestBodyContainingRule, m_RequestBodyMediaType, m_RequestBodyEncoding,
                       m_QueryParams, m_Headers,
                       responses, m_Tracker);
        }

        private async Task<string?> SerialiseExpectedRequestMatchBody()
        {
            if (m_RequestBodyContent != null)
                return await m_RequestBodyContent.ReadContentAsync();

            return m_RequestBody == null
                ? null
                : JsonSerializer.Serialize(m_RequestBody, m_Options.RequestBodySerializerOptions);
        }

        private List<Response> CreateResponses()
        {
            var responseBuilder = m_ResponseBuilder;

            if (responseBuilder == null)
                return [ResponseBuilder.DefaultResponse(m_Options)];

            var responsesInChain = responseBuilder.TraverseAndCountResponseChain();
            var responses = new List<Response>(responsesInChain) { responseBuilder.ToHttpResponse() };

            while (responseBuilder?.NextResponse != null)
            {
                responseBuilder = responseBuilder.NextResponse;
                responses.Add(responseBuilder.ToHttpResponse());
            }

            return responses;
        }

        #endregion
    }
}