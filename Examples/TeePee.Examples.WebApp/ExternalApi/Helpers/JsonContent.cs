using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace TeePee.Examples.WebApp.ExternalApi.Helpers
{
    public class JsonContent<T> : StringContent
    {
        public JsonContent(T content) : base(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
        {
        }
    }
}