using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace TeePee.Refit.Tests
{
    public class EndToEndTests : IClassFixture<EndToEndTestsFixture>
    {
        private readonly EndToEndTestsFixture m_Fixture;

        public EndToEndTests(EndToEndTestsFixture fixture)
        {
            m_Fixture = fixture;
            m_Fixture.SetTestScope();
        }

        [Fact]
        public async Task AttachToRefitInterface_AllowsTeePeeToConfigureResponse()
        {
            // Given
            m_Fixture.TeePeeBuilder.ForRequest("https://api.unit-test.com/users/abc-123", HttpMethod.Get)
                   .Responds()
                   .WithBody(new { Name = "User's Name" })
                   .WithStatus(System.Net.HttpStatusCode.OK);

            var controller = m_Fixture.GetSUT();

            // When
            var users = await controller.GetUser("abc-123");

            // Then
            Assert.Single(users);
            Assert.Equal("User's Name", users[0].Name);
        }

        [Fact]
        public async Task AttachToRefitInterface_RulesAreIsolatedPerTest()
        {
            // Given
            // Nothing configured

            var controller = m_Fixture.GetSUT();

            // When
            Func<Task<IReadOnlyList<User>>> func = async () => await controller.GetUser("abc-123");

            // Then
            await Assert.ThrowsAsync<ApiException>(func);
        }
    }

    public class EndToEndTestsFixture : BaseFixture<ExampleControllerUnderTest>
    {
        public TeePeeBuilder TeePeeBuilder { get; } = new();

        protected override IServiceCollection ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddExampleController(configuration);
            services.AttachToRefitInterface<IApiService>(TeePeeBuilder);
            return services;
        }

        protected override void Reset()
        {
            TeePeeBuilder.Reset();
        }
    }

    public abstract class BaseFixture<T> : IDisposable where T : class
    {
        private readonly IServiceCollection m_Services;

        private IServiceProvider? m_ServiceProvider;

        private IServiceScope? m_Scope;

        public BaseFixture()
        {
            var config = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.unittests.json")
                        .Build();

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

    public class ExampleControllerUnderTest
    {
        private readonly ExampleIntermediateSingletonService m_IntermediateService;

        public ExampleControllerUnderTest(ExampleIntermediateSingletonService intermediateService)
        {
            m_IntermediateService = intermediateService;
        }

        public async Task<IReadOnlyList<User>> GetUser(params string[] userIds)
        {
            List<User> users = new(userIds.Length);
            foreach (var userId in userIds)
            {
                users.Add(await m_IntermediateService.GetUser(userId));
            }
            return users;
        }
    }

    public interface IApiService
    {
        [Get("/users/{user}")]
        Task<User> GetUser(string user);
    }

    public record User(string Name);

    public class ExampleIntermediateSingletonService
    {
        private readonly IApiService m_ApiService;

        public ExampleIntermediateSingletonService(IApiService apiService)
        {
            m_ApiService = apiService;
        }

        public async Task<User> GetUser(string userId)
        {
            return await m_ApiService.GetUser(userId);
        }
    }

    public static class ExampleControllerProductionStartup
    {
        public static IServiceCollection AddExampleController(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ExampleControllerUnderTest>(); // Normally registered by ASP.NET startup, but simulating here.
            services.AddSingleton<ExampleIntermediateSingletonService>();

            services.AddRefitClient<IApiService>()
                    .ConfigureHttpClient(c => c.BaseAddress = new Uri(configuration.GetSection("Api").GetValue<string>("BaseUrl")!));
            return services;
        }
    }
}