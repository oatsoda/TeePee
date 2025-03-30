using System.Text.Json;

namespace TeePee.Examples.WebApp.ExternalApi.Helpers
{
    public static class HttpResponseExtensions
    {
        public static async Task<T> DeserialiseTo<T>(this HttpResponseMessage httpResponseMessage)
        {
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content)!; // Will not deserialise to null
        }
    }
}