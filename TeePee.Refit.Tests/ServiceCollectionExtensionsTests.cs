using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace TeePee.Refit.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        public interface IApiService
        {
            [Get("/users/{user}")]
            Task<User> GetUser(string user);
        }

        public record User(string Name);

        [Fact]
        public async Task AttachToRefitInterfaceInjectsTeePeeMocksIntoRefitInterface()
        {
            // Given


            // - Production Code Setup -
            var services = new ServiceCollection();
            services.AddRefitClient<IApiService>()
                    .ConfigureHttpClient(c => c.BaseAddress = new("https://api.github.com"));

            // - Test Setup -
            var builder = new TeePeeBuilder();
            builder.ForRequest("https://api.github.com/users/abc-123", HttpMethod.Get)
                   .Responds()
                   .WithBody(new { Name = "User's Name" })
                   .WithStatus(HttpStatusCode.OK);


            // When


            // - Subject Under Test -
            services.AttachToRefitInterface<IApiService>(builder.Build());

            // - Simulate Production Code - 
            var user = await services.BuildServiceProvider().GetRequiredService<IApiService>().GetUser("abc-123");


            // Then
            Assert.Equal("User's Name", user.Name);
        }
    }
}