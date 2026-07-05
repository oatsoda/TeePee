using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TeePee.Examples.WebApp.Tests.FixtureScopedTests
{
    public abstract class BaseFixture<T> : IDisposable where T : class
    {
        private readonly IServiceCollection m_Services;

        private IServiceProvider? m_ServiceProvider;

        private IServiceScope? m_Scope;

        public BaseFixture()
        {
            var config = UnitTestConfig.LoadUnitTestConfig();

            m_Services = new ServiceCollection()
                .AddScoped<T>();

            ConfigureServices(m_Services, config);
        }

        public void SetTestScope()
        {
            if (m_Scope == null)
            {
                // First test execution
                m_ServiceProvider = m_Services.BuildServiceProvider();
            }
            else
            {
                m_Scope.Dispose();
                Reset();
            }

            m_Scope = m_ServiceProvider!.CreateScope();
        }

        protected abstract IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration);
        protected abstract void Reset();

        public T GetSUT() => m_Scope == null
            ? throw new InvalidOperationException("Test scope is not set.")
            : m_Scope.ServiceProvider.GetRequiredService<T>();

        public void Dispose()
        {
            m_Scope?.Dispose();
        }
    }
}
