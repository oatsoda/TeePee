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
            // StringContent is derived from Byte Array, but will know the encoding to read as string, but byte array 
            if (httpContent is ByteArrayContent byteContent and not StringContent)
                return Convert.ToBase64String(await byteContent.ReadAsByteArrayAsync());
                
            return await httpContent.ReadAsStringAsync();
        }
    }
}