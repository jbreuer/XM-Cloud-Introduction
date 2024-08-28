
using System.Net.Http.Headers;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Extensions;
using Sitecore.AspNetCore.SDK.LayoutService.Client.Serialization;

namespace LayoutService
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new GraphQLRequestConverter());
                    options.JsonSerializerOptions.AddLayoutServiceDefaults();
                });

            services.AddSingleton<ISitecoreLayoutSerializer, JsonLayoutServiceSerializer>();
            services.AddScoped<LayoutServiceHelper>();
            
            services.AddHttpClient("httpClient", client =>
            {
                client.BaseAddress = new Uri("https://xmcloudcm.localhost/sitecore/api/layout/render/jss");
            });
            
            // WeatherService HttpClient
            services.AddHttpClient<WeatherService>(client =>
            {
                client.BaseAddress = new Uri("https://api.openweathermap.org/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
            
            services.AddMemoryCache();

            services.AddScoped(x => new GraphQLHttpClient("https://xmcloudcm.localhost/sitecore/api/graph/edge", new SystemTextJsonSerializer()));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
                
                endpoints.MapControllerRoute(
                    "layout",
                    "/sitecore/api/layout/render/jss",
                    new { controller = "Layout", action = "Item" }
                );
                
                endpoints.MapControllerRoute(
                    "graph",
                    "/sitecore/api/graph/edge",
                    new { controller = "Graph", action = "Index" }
                );
            });
        }
    }
}