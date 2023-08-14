using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TeePee.Examples.WebApp.ExternalApi;
using TeePee.Examples.WebApp.ExternalApi.Helpers;

namespace TeePee.Examples.WebApp.Controllers
{
    /// <summary>
    /// Examples of using Multiple Named IHttpClientFactory usage.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class HttpClientFactoryMultipleNamedUsageController : ControllerBase
    {
        public const string HTTP_CLIENT_NAME_ONE = "OneApi";
        public const string HTTP_CLIENT_NAME_TWO = "TwoApi";

        private readonly IHttpClientFactory m_HttpClientFactory;

        public HttpClientFactoryMultipleNamedUsageController(IHttpClientFactory httpClientFactory)
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
            var httpClientOne = m_HttpClientFactory.CreateClient(HTTP_CLIENT_NAME_ONE);
            var httpResponseMessageOne = await httpClientOne.GetAsync("/path-one/resource-one?filter=those");
            var resultOne = await httpResponseMessageOne.DeserialiseTo<ThirdPartyResponseModel>();
            
            var httpClientTwo = m_HttpClientFactory.CreateClient(HTTP_CLIENT_NAME_TWO);
            var httpResponseMessageTwo = await httpClientTwo.GetAsync("/path-two/resource-two?filter=those");
            var resultTwo = await httpResponseMessageTwo.DeserialiseTo<ThirdPartyResponseModel>();

            return Ok(resultOne.Things.Single().Value + resultTwo.Things.Single().Value);
        }

        /// <summary>
        /// Example of less common fire and forget calls to HttpClient - tests cannot use controller method result to determine outcome.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> FireAndForget()
        {
            var httpClientOne = m_HttpClientFactory.CreateClient(HTTP_CLIENT_NAME_ONE);
            var requestBodyOne = JsonContent.Create(new ThirdPartyRequestModel { Caller = "ThisCaller" });
            await httpClientOne.PutAsync("/path-one/resource-one?filter=other", requestBodyOne);
            
            var httpClientTwo = m_HttpClientFactory.CreateClient(HTTP_CLIENT_NAME_TWO);
            var requestBodyTwo = JsonContent.Create(new ThirdPartyRequestModel { Caller = "ThisCaller" });
            await httpClientTwo.PutAsync("/path-two/resource-two?filter=other", requestBodyTwo);

            return Ok();
        }

    }
}
