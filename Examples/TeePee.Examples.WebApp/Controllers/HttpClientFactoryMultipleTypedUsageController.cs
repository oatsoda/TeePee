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
    public class HttpClientFactoryMultipleTypedUsageController : ControllerBase
    {
        private readonly ExampleTypedHttpClient m_Dependency;
        private readonly AnotherExampleTypedHttpClient m_AnotherDependency;

        public HttpClientFactoryMultipleTypedUsageController(ExampleTypedHttpClient dependency, AnotherExampleTypedHttpClient anotherDependency)
        {
            m_Dependency = dependency;
            m_AnotherDependency = anotherDependency;
        }

        /// <summary>
        /// Example showing most common usage of HttpClient - making a call and using the result.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> FireAndAct()
        {
            var resultOne = await m_Dependency.GetSingleValue();
            var resultTwo = await m_AnotherDependency.GetSingleValue();
            return Ok(resultOne + resultTwo);
        }

        /// <summary>
        /// Example of less common fire and forget calls to HttpClient - tests cannot use controller method result to determine outcome.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> FireAndForget()
        {
            await m_Dependency.Put();
            await m_AnotherDependency.Put();
            return Ok();
        }

    }

    public class AnotherExampleTypedHttpClient
    {
        public HttpClient HttpClient { get; }

        public AnotherExampleTypedHttpClient(HttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        public async Task<int> GetSingleValue()
        {
            var httpResponseMessage = await HttpClient.GetAsync("/path/otherresource?filter=those");
            var result = await httpResponseMessage.DeserialiseTo<ThirdPartyResponseModel>();
            return result.Things.Single().Value;
        }

        public async Task Put()
        {
            var requestBody = new JsonContent<ThirdPartyRequestModel>(new ThirdPartyRequestModel { Caller = "ThisCaller" });
            await HttpClient.PutAsync("/path/otherresource?filter=other", requestBody);
        }
    }
}
