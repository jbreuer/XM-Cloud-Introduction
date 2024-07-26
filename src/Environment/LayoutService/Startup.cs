using System.Net.Http.Headers;
using System.Web;
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
            
            services.Configure<HttpLayoutRequestHandlerOptions>("httpClient", options =>
            {
                options.RequestMap.Add((request, message) =>
                {
                    var uriBuilder = new UriBuilder(message.RequestUri);
                    var query = HttpUtility.ParseQueryString(uriBuilder.Query);

                    query["sc_apikey"] = "{E2F3D43E-B1FD-495E-B4B1-84579892422A}";
                    query["sc_site"] = "mvp-site";
                    query["sc_lang"] = "en";

                    uriBuilder.Query = query.ToString();
                    message.RequestUri = uriBuilder.Uri;
                });

                options.RequestMap.Add((request, message) =>
                {
                    message.RequestUri = request.BuildDefaultSitecoreLayoutRequestUri(message.RequestUri);

                    if (request.TryReadValue<string>("sc_auth_header_key", out var parameter))
                    {
                        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", parameter);
                    }

                    if (request.TryGetHeadersCollection(out var headers))
                    {
                        foreach (var header in headers)
                        {
                            if (new List<string>().Contains(header.Key)) // Add your non-validated headers here
                            {
                                message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                            }
                            else
                            {
                                message.Headers.Add(header.Key, header.Value);
                            }
                        }
                    }
                });
            });
            
            services.AddHttpClient("httpClient", client =>
            {
                client.BaseAddress = new Uri("https://xmcloudcm.localhost/sitecore/api/layout/render/jss");
            });
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