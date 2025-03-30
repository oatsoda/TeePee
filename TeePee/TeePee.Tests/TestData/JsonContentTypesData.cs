using System.Text;

namespace TeePee.Tests.TestData
{
    public class JsonContentTypesData : TheoryData<string, Encoding>
    {
        public JsonContentTypesData()
        {
            Add("text/plain", Encoding.UTF8);
            Add("application/json", Encoding.UTF8);
            Add("text/plain", Encoding.ASCII);
        }
    }
}