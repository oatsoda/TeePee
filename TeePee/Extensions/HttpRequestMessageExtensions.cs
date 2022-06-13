using System.Net.Http;
using System.Threading.Tasks;

namespace TeePee.Extensions
{
    public static class HttpRequestMessageExtensions
    {
        public static async Task<string> ReadContentAsync(this HttpRequestMessage httpRequestMessage)
        {
            if (httpRequestMessage.Content == null)
                return null;

            return await httpRequestMessage.Content.ReadAsStringAsync();
        }
    }
}