namespace TeePee.Built
{
    public class TeePeeManual
    {
        private readonly TeePeeBuilder m_Builder;
        private readonly Uri? m_BaseAddressForHttpClient;

        internal TeePeeManual(TeePeeBuilder teePeeBuilder, string? baseAddressForHttpClient)
        {
            m_Builder = teePeeBuilder;
            m_BaseAddressForHttpClient = baseAddressForHttpClient == null ? null : new Uri(baseAddressForHttpClient);
        }

        /// <summary>
        /// Manually creates the HttpClient so you can pass it in to a subject-under-test.
        /// </summary>
        public HttpClient CreateClient()
        {
            var handler = new TeePeeMessageHandler(m_Builder);
            return m_BaseAddressForHttpClient == null
                ? new(handler)
                : new HttpClient(handler) { BaseAddress = m_BaseAddressForHttpClient };
        }

        /// <summary>
        /// Manually creates an IHttpClientFactory which will resolve an HttpClient for the given Name, so you can pass it
        /// in to a subject-under-test.
        /// </summary>
        /// <param name="clientName">The Expected Name of the HttpClient. If your SUT is not passing this name, then IHttpClientFactory
        /// will throw during execution.</param>
        public IHttpClientFactory CreateHttpClientFactory(string clientName) => new TeePeeFakeHttpClientFactory(clientName, CreateClient());
    }

    public class TeePeeFakeHttpClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClient> m_HttpClients = [];

        internal TeePeeFakeHttpClientFactory(string clientName, HttpClient httpClient)
        {
            m_HttpClients.Add(clientName, httpClient);
        }

        internal TeePeeFakeHttpClientFactory((string ClientName, HttpClient HttpClient)[] clientInfo)
        {
            foreach (var info in clientInfo)
            {
                m_HttpClients.Add(info.ClientName, info.HttpClient);
            }
        }

        public HttpClient CreateClient(string name)
        {
            // Force callers to specify correct named instance
            return m_HttpClients.TryGetValue(name, out var httpClient)
                ? httpClient
                : throw new ArgumentOutOfRangeException(nameof(name), $"No HttpClients configured with name '{name}'. Configured with {string.Join(", ", m_HttpClients.Keys.Select(c => $"'{c}'"))}");
        }
    }
}
