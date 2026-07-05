using Microsoft.Extensions.Configuration;

namespace TeePee.Examples.WebApp.Tests
{
    public static class UnitTestConfig
    {
        public static IConfiguration LoadUnitTestConfig() => new ConfigurationBuilder()
                                                            .AddJsonFile("appsettings.unittests.json")
                                                            .Build();
    }
}
