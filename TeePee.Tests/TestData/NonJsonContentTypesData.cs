using System.Collections.Generic;
using System.Net.Http;
using System.Text;
// ReSharper disable UseUtf8StringLiteral

namespace TeePee.Tests.TestData
{
    public class NonJsonContentTypesData : BaseData
    {
        public override IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { new ByteArrayContent(new byte[]{ 65, 98, 48 }) };
            yield return new object[] { new ByteArrayContent(new byte[]{ 65, 98, 48 })
                                        {
                                            Headers = { ContentType = new("img/jpeg") { CharSet = Encoding.UTF8.WebName } }
                                        }
                                      };
            yield return new object[] { new StringContent("here's my content", Encoding.UTF8) };
            yield return new object[] { new StringContent("here's my content", Encoding.ASCII) };
            yield return new object[] { new MultipartFormDataContent
                                        {
                                            new StringContent("some string"),
                                            new ByteArrayContent(new byte[]{ 65, 98, 48 })
                                        } };
            yield return new object[] { new FormUrlEncodedContent(
                                                                  new Dictionary<string, string>
                                                                  {
                                                                      { "k1", "v1" },
                                                                      { "k2", "v2" },
                                                                  })
                                      };
        }
    }
}