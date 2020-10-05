using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TeePee.Examples.WebApp.Controllers
{
    /// <summary>
    /// Examples of using Basic IHttpClientFactory usage.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HttpClientFactoryManualInjectionBasicUsageController : ControllerBase
    {
        private readonly IHttpClientFactory m_HttpClientFactory;

        public HttpClientFactoryManualInjectionBasicUsageController(IHttpClientFactory httpClientFactory)
        {
            m_HttpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Example showing most common usage of HttpClient - making a call and using the result.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> FireAndAct()
        {
            var httpClient = m_HttpClientFactory.CreateClient();
            var httpResponseMessage = await httpClient.GetAsync("https://some.api/path/resource?filter=those");
            var result = await httpResponseMessage.DeserialiseTo<ThirdPartyResponseModel>();
            
            return Ok(result.Things.Single().Value);
        }

        /// <summary>
        /// Example of less common fire and forget calls to HttpClient - tests cannot use controller method result to determine 
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> FireAndForget()
        {
            var httpClient = m_HttpClientFactory.CreateClient();
            var requestBody = new JsonContent<ThirdPartyRequestModel>(new ThirdPartyRequestModel { Caller = "ThisCaller" });
            await httpClient.PutAsync("https://some.api/path/resource?filter=other", requestBody);
            return Ok();
        }

    }

    public class ThirdPartyResponseModel
    {
        public ThirdPartyResponseObj[] Things { get; set; }

        public class ThirdPartyResponseObj
        {
            public int Value { get; set; }
        }
    }

    public class ThirdPartyRequestModel
    {
        public string Caller { get; set; }
    }

    public class JsonContent<T> : StringContent
    {
        public JsonContent(T content) : base(JsonSerializer.Serialize(content), Encoding.UTF8, "application/json")
        {
        }
    }

    public static class HttpResponseExtensions
    {
        public static async Task<T> DeserialiseTo<T>(this HttpResponseMessage httpResponseMessage)
        {
            var content = await httpResponseMessage.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }
    }
}
