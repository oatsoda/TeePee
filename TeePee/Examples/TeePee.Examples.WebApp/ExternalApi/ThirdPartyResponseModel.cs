namespace TeePee.Examples.WebApp.ExternalApi
{
    public class ThirdPartyResponseModel
    {
        public required ThirdPartyResponseObj[] Things { get; set; }

        public class ThirdPartyResponseObj
        {
            public int Value { get; set; }
        }
    }
}