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
        /// </summary>
        public static TeePeeManual Manual(this TeePeeBuilder teePeeBuilder, string? baseAddressForHttpClient = null)
        {
            return new(teePeeBuilder, baseAddressForHttpClient);
        }
    }
}
