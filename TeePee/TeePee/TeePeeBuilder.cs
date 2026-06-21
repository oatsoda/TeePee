using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;
using TeePee.Internal;

namespace TeePee
{
    public class TeePeeBuilder
    {
        private readonly TeePeeOptions m_Options = new();

        private readonly List<RequestMatchBuilder> m_Requests = new();

        private HttpStatusCode m_DefaultResponseStatusCode = HttpStatusCode.NotFound;
        private string? m_DefaultResponseBody;

        private bool m_IsBuilt;

        //public string? HttpClientNamedInstance { get; }

        public TeePeeBuilder() : this(null, null) { }

        public TeePeeBuilder(JsonSerializerOptions responseBodySerializeOptions) : this(opt => opt.ResponseBodySerializerOptions = responseBodySerializeOptions) { }
        //public TeePeeBuilder(string httpClientNamedInstance) : this(null, httpClientNamedInstance) { }

        public TeePeeBuilder(Action<TeePeeOptions>? setOptions = null, string? httpClientNamedInstance = null)
        {
            if (setOptions != null)
                setOptions(m_Options);

            //HttpClientNamedInstance = httpClientNamedInstance;
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
                throw new InvalidOperationException("Cannot add more request tracking after builder has been built.");

            var builder = new RequestMatchBuilder(this, m_Options, url, httpMethod);
            // Note: This assumes valid before adding
            m_Requests.Add(builder);
            return builder;
        }

        // TODO: This should be internal, but public only for Manual cases, so could explicitly create Manual method? Or change the return of this?
        public async Task<TeePee> Build(ILogger<TeePee>? logger = null)
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
            return new(m_Options, requestMatchRulesOrdered, m_DefaultResponseStatusCode, m_DefaultResponseBody, logger);
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
    }

    public static class ResolveExtensions
    {
        public static IServiceCollection AttachToDefaultClient(this IServiceCollection services, TeePeeBuilder teePeeBuilder)
        {
            return AttachToNamedClient(services, teePeeBuilder, Options.DefaultName);
        }

        public static IServiceCollection AttachToNamedClient(this IServiceCollection services, TeePeeBuilder teePeeBuilder, string clientName)
        {
            // We expect this to be called only once per Builder? Per-Fixture is expected; Per-Test, would you
            // be using DI? Maybe but a new Bulder + Service Collection would be created per test - so isolated.
            // So YES, the Builder would expect to be "attached" only once and not expect a TeePeeMessageHandler to already exist.

            // TODO: So I would need to enforce NOT being able to call this twice?

            // register the test handler in DI - DO I NEED TO? Will be problematic with multiple TeePeeBuilders for multiple named/typed clients in SUT.
            //services.AddTransient<THandler>();

            // inject the handler into the existing named client configuration
            services.Configure<HttpClientFactoryOptions>(clientName, options =>
            {
                options.HttpMessageHandlerBuilderActions.Add(builder =>
                {
                    // resolve handler from the builder's IServiceProvider and add it to the pipeline
                    //var handler = (DelegatingHandler)builder.Services.GetRequiredService<THandler>();

                    var handler = teePeeBuilder.Build().GetAwaiter().GetResult().HttpHandler;
                    builder.AdditionalHandlers.Add(handler);
                });
            });

            return services;
        }

        public static IServiceCollection AttachToTypedClient<TClient>(this IServiceCollection services, TeePeeBuilder teePeeBuilder)
        {
            return AttachToNamedClient(services, teePeeBuilder, typeof(TClient).Name!);
        }

        //public static HttpClient CreateClient(this TeePeeBuilder teePeeBuilder, string? baseAddressForHttpClient = null)
        //{
        //    var handler = teePeeBuilder.Build().GetAwaiter().GetResult().HttpHandler;

        //    return baseAddressForHttpClient == null
        //        ? new(handler)
        //        : new HttpClient(handler) { BaseAddress = new Uri(baseAddressForHttpClient) };
        //}

        //// TODO: Multiple
        //public static IHttpClientFactory CreateHttpClientFactory(this TeePeeBuilder teePeeBuilder, string clientName, string? baseAddressForHttpClient = null)
        //{
        //    var handler = teePeeBuilder.Build().GetAwaiter().GetResult().HttpHandler;

        //    return baseAddressForHttpClient == null
        //        ? new(handler)
        //        : new HttpClient(handler) { BaseAddress = new Uri(baseAddressForHttpClient) };
        //}
    }

    //public class TeePeeBuilder<TClient> : TeePeeBuilder
    //{
    //    public Type TypedClientType => typeof(TClient);
    //}
}
