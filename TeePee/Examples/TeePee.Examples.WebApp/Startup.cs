using Microsoft.OpenApi;
using TeePee.Examples.WebApp.Controllers;

namespace TeePee.Examples.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHttpClient();
            services.AddNamedHttpClients(Configuration);
            services.AddTypedHttpClients(Configuration);

            // Swagger
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "TeePee Examples WebApp API",
                    Version = "v1",
                    Description = "Example endpoints for TeePee"
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            // Enable middleware to serve generated Swagger as JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TeePee Examples WebApp API v1");
                // Serve the UI at the app's root (optional)
                // c.RoutePrefix = string.Empty;
            });

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public static class StartupDependencyExtensions
    {
        // Separate out startup registrations so that your unit tests can setup the same dependencies

        public static IServiceCollection AddNamedHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient(HttpClientFactoryNamedUsageController.HTTP_CLIENT_NAME, c => c.BaseAddress = new Uri(configuration.GetSection("ExampleNamedClient").GetValue<string>("BaseUrl")!));
            services.AddHttpClient(HttpClientFactoryMultipleNamedUsageController.HTTP_CLIENT_NAME_ONE, c => c.BaseAddress = new Uri(configuration.GetSection("MultipleNamedClientOne").GetValue<string>("BaseUrl")!));
            services.AddHttpClient(HttpClientFactoryMultipleNamedUsageController.HTTP_CLIENT_NAME_TWO, c => c.BaseAddress = new Uri(configuration.GetSection("MultipleNamedClientTwo").GetValue<string>("BaseUrl")!));
            return services;
        }

        public static IServiceCollection AddTypedHttpClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<ExampleTypedHttpClient>(c => c.BaseAddress = new Uri(configuration.GetSection("ExampleTypedHttpClient").GetValue<string>("BaseUrl")!));
            services.AddHttpClient<AnotherExampleTypedHttpClient>(c => c.BaseAddress = new Uri(configuration.GetSection("AnotherExampleTypedHttpClient").GetValue<string>("BaseUrl")!));
            return services;
        }
    }
}
