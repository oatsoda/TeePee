using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace TeePee.Tests.TestData
{
    public class NonJsonContentTypesData : BaseData
    {
        public override IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new ByteArrayContent(new byte[]{ 65, 98, 48 }) };
            yield return new object[] { new StringContent("here's my content", Encoding.UTF8) };
            yield return new object[] { new StringContent("here's my content", Encoding.ASCII) };
        }
    }
}