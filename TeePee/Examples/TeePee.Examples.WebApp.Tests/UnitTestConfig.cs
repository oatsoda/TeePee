using Microsoft.Extensions.Configuration;
// ReSharper disable StringLiteralTypo

namespace TeePee.Examples.WebApp.Tests
{
    public static class UnitTestConfig
    {
        public static IConfiguration LoadUnitTestConfig() => new ConfigurationBuilder()
                                                            .AddJsonFile("appsettings.unittests.json")
                                                            .Build();
    }
}
