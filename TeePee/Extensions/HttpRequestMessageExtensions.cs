using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TeePee.Extensions
{
    internal static class HttpRequestMessageExtensions
    {
        public static async Task<string?> ReadContentAsync(this HttpRequestMessage httpRequestMessage)
        {
            if (httpRequestMessage.Content == null)
                return null;

            return await httpRequestMessage.Content.ReadContentAsync();
        }
        
        public static async Task<string?> ReadContentAsync(this HttpContent httpContent)
        {
            if (httpContent.GetType() == typeof(ByteArrayContent))
                return Convert.ToBase64String(await httpContent.ReadAsByteArrayAsync());
                
            return await httpContent.ReadAsStringAsync();
        }
    }
}