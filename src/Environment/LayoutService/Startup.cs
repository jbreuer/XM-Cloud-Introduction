using System.Net.Http.Headers;
using System.Web;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Newtonsoft;
using Sitecore.LayoutService.Client.Request;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.LayoutService.Client.Newtonsoft.Extensions;

namespace LayoutService
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {   
            services
                .AddControllers()
                .AddNewtonsoftJson(o => o.SerializerSettings.SetDefaults());;

            services.AddSingleton<ISitecoreLayoutSerializer, NewtonsoftLayoutServiceSerializer>();
            services.AddScoped<LayoutServiceHelper>();
            
            services.AddHttpClient("httpClient", client =>
            {
                client.BaseAddress = new Uri("https://xmcloudcm.localhost/sitecore/api/layout/render/jss");
            });

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
                    "graph",
                    new { controller = "Graph", action = "Index" }
                );
            });
        }
    }
}