using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;
using TeePee.Examples.WebApp.ExternalApi;
using TeePee.Examples.WebApp.ExternalApi.Helpers;

namespace TeePee.Examples.WebApp.Controllers
{
    /// <summary>
    /// Examples of using Basic IHttpClientFactory usage.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HttpClientFactoryBasicUsageController : ControllerBase
    {
        private readonly IHttpClientFactory m_HttpClientFactory;

        public HttpClientFactoryBasicUsageController(IHttpClientFactory httpClientFactory)
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
        /// Example of less common fire and forget calls to HttpClient - tests cannot use controller method result to determine outcome.
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
}
