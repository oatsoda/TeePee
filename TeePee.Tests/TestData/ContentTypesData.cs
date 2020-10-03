using System.Collections.Generic;
using System.Text;

namespace TeePee.Tests.TestData
{
    public class ContentTypesData : BaseData
    {
        public override IEnumerator<object[]> GetEnumerator()
        {
            yield return new object[] { "text/plain", Encoding.UTF8 };
            yield return new object[] { "application/json", Encoding.UTF8 };
            yield return new object[] { "text/plain", Encoding.ASCII };
        }
    }
}