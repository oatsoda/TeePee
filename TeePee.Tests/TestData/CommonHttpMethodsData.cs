using System.Collections;
using System.Collections.Generic;
using System.Net.Http;

namespace TeePee.Tests.TestData
{
    public class CommonHttpMethodsData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { HttpMethod.Get };
            yield return new object[] { HttpMethod.Post };
            yield return new object[] { HttpMethod.Put };
            yield return new object[] { HttpMethod.Delete };
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}