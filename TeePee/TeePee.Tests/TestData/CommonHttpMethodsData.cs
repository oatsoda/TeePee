using System.Net.Http;
using Xunit;

namespace TeePee.Tests.TestData
{
    public class CommonHttpMethodsData : TheoryData<HttpMethod>
    {
        public CommonHttpMethodsData()
        {
            Add(HttpMethod.Get);
            Add(HttpMethod.Post);
            Add(HttpMethod.Put);
            Add(HttpMethod.Delete);
        }
    }
}