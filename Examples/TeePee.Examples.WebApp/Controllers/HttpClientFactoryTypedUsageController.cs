using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using TeePee.Examples.WebApp.ExternalApi;
using TeePee.Examples.WebApp.ExternalApi.Helpers;

namespace TeePee.Examples.WebApp.Controllers
{
    /// <summary>
    /// Examples of using a Typed IHttpClientFactory usage.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HttpClientFactoryTypedUsageController : ControllerBase
    {
        private readonly ExampleTypedHttpClient m_Dependency;

        public HttpClientFactoryTypedUsageController(ExampleTypedHttpClient dependency)
        {
            m_Dependency = dependency;
        }

        /// <summary>
        /// Example showing most common usage of HttpClient - making a call and using the result.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> FireAndAct()
        {
            var result = await m_Dependency.GetSingleValue();
            return Ok(result);
        }

        /// <summary>
        /// Example of less common fire and forget calls to HttpClient - tests cannot use controller method result to determine outcome.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> FireAndForget()
        {
            await m_Dependency.Put();
            return Ok();
        }

    }

    public class ExampleTypedHttpClient
    {
        public HttpClient HttpClient { get; }

        public ExampleTypedHttpClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public async Task<int> GetSingleValue()
        {
            var httpResponseMessage = await HttpClient.GetAsync("/path/resource?filter=those");
            var result = await httpResponseMessage.DeserialiseTo<ThirdPartyResponseModel>();
            return result.Things.Single().Value;
        }

        public async Task Put()
        {
            var requestBody = new JsonContent<ThirdPartyRequestModel>(new ThirdPartyRequestModel { Caller = "ThisCaller" });
            await HttpClient.PutAsync("/path/resource?filter=other", requestBody);
        }
    }
}
