using System.Text;
// ReSharper disable UseUtf8StringLiteral

namespace TeePee.Tests.TestData
{
    public class NonJsonContentTypesData : TheoryData<HttpContent>
    {
        public NonJsonContentTypesData()
        {
            Add((HttpContent)new ByteArrayContent([65, 98, 48]));
            Add((HttpContent)new ByteArrayContent([65, 98, 48])
            {
                Headers = { ContentType = new("img/jpeg") { CharSet = Encoding.UTF8.WebName } }
            });

            Add((HttpContent)new StringContent("here's my content", Encoding.UTF8));
            Add((HttpContent)new StringContent("here's my content", Encoding.ASCII));

            Add((HttpContent)new MultipartFormDataContent
                                {
                                    new StringContent("some string"),
                                    new ByteArrayContent([65, 98, 48])
                                });

            Add((HttpContent)new FormUrlEncodedContent(
                                new Dictionary<string, string>
                                {
                                    { "k1", "v1" },
                                    { "k2", "v2" },
                                }));
        }
    }
}