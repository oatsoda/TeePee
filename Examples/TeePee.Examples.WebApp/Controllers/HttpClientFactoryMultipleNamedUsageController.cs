using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
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
        private const string _HTTP_CLIENT_NAME_ONE = "OneApi";
        private const string _HTTP_CLIENT_NAME_TWO = "TwoApi";

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
            var httpClientOne = m_HttpClientFactory.CreateClient(_HTTP_CLIENT_NAME_ONE);
            var httpResponseMessageOne = await httpClientOne.GetAsync("https://first.api/pathone/resourceone?filter=those");
            var resultOne = await httpResponseMessageOne.DeserialiseTo<ThirdPartyResponseModel>();
            
            var httpClientTwo = m_HttpClientFactory.CreateClient(_HTTP_CLIENT_NAME_TWO);
            var httpResponseMessageTwo = await httpClientTwo.GetAsync("https://second.api/pathtwo/resourcetwo?filter=those");
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
            var httpClientOne = m_HttpClientFactory.CreateClient(_HTTP_CLIENT_NAME_ONE);
            var requestBodyOne = new JsonContent<ThirdPartyRequestModel>(new ThirdPartyRequestModel { Caller = "ThisCaller" });
            await httpClientOne.PutAsync("https://first.api/pathone/resourceone?filter=other", requestBodyOne);
            
            var httpClientTwo = m_HttpClientFactory.CreateClient(_HTTP_CLIENT_NAME_TWO);
            var requestBodyTwo = new JsonContent<ThirdPartyRequestModel>(new ThirdPartyRequestModel { Caller = "ThisCaller" });
            await httpClientTwo.PutAsync("https://second.api/pathtwo/resourcetwo?filter=other", requestBodyTwo);

            return Ok();
        }

    }
}
