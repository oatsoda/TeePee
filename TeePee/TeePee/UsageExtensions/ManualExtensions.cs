using TeePee.Built;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace TeePee
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    public static class ManualExtensions
    {
        /// <summary>
        /// For situations where you want to manually inject HttpClient or HttpClientFactory into your test subjects. Otherwise
        /// use the <see type="IServiceCollection">IServiceCollection</see> Attach... extensions to use real DI.
        /// <br /><br />
        /// Specifying the <c>baseAddressForHttpClient</c> is an implementation detail and a side-effect of using the manual
        /// injection for tests. If your SUT is calling HttpClient with relative paths then you'll need to match the Base URL
        /// of the Test HttpClient with the URL used in the TeePeeBuilder. This is somewhat tautological and not really proving anything.
        /// </summary>
        public static TeePeeManual Manual(this TeePeeBuilder teePeeBuilder, string? baseAddressForHttpClient = null)
        {
            return new(teePeeBuilder, baseAddressForHttpClient);
        }

        /// <summary>
        /// For situations where you want to manually inject a HttpClientFactory into your test subjects, where multiple
        /// named HttpClients are used.
        /// </summary>
        public static TeePeeFakeHttpClientFactory ToHttpClientFactory(this (string ClientName, TeePeeManual TeePeeManual)[] teePeeManuals)
        {
            var clients = teePeeManuals.Select(m => (m.ClientName, m.TeePeeManual.CreateClient())).ToArray();
            return new TeePeeFakeHttpClientFactory(clients);
        }
    }
}
