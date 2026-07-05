using Microsoft.Extensions.DependencyInjection;
using TeePee.Built;

namespace TeePee.UsageExtensions
{
    public static class InternalSingletonHandlerExtensions
    {
        internal static IHttpClientBuilder AddSingletonTeePeeMessageHandler(this IHttpClientBuilder httpClientBuilder, TeePeeBuilder teePeeBuilder)
        {
            // Always return same instance of handler, so keep a reference here. Not thread-safe though.
            TeePeeMessageHandler? requestHandler = null;
            return httpClientBuilder.AddHttpMessageHandler(_ => requestHandler ??= new TeePeeMessageHandler(teePeeBuilder));
        }
    }
}
