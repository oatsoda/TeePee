namespace TeePee.Examples.WebApp.ExternalApi
{
    public class ThirdPartyResponseModel
    {
        public ThirdPartyResponseObj[] Things { get; set; }

        public class ThirdPartyResponseObj
        {
            public int Value { get; set; }
        }
    }
}