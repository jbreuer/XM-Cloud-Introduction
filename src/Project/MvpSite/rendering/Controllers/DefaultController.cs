using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mvp.Project.MvpSite.Middleware;
using Mvp.Project.MvpSite.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Okta.AspNetCore;
using Sitecore.AspNet.RenderingEngine;
using Sitecore.LayoutService.Client;
using Sitecore.LayoutService.Client.Exceptions;
using Sitecore.LayoutService.Client.Newtonsoft;
using Sitecore.LayoutService.Client.Newtonsoft.Converters;
using Sitecore.LayoutService.Client.Newtonsoft.Model;
using Sitecore.LayoutService.Client.Request;
using Sitecore.LayoutService.Client.Response.Model;
using Sitecore.LayoutService.Client.Response.Model.Fields;

namespace Mvp.Project.MvpSite.Controllers
{
    public class DefaultController : Controller
    {
        private readonly ISitecoreLayoutClient _layoutClient;
        private readonly ISitecoreLayoutSerializer _serializer;
        private readonly ILogger<DefaultController> _logger;

        public DefaultController(ISitecoreLayoutClient layoutClient, ISitecoreLayoutSerializer serializer, ILogger<DefaultController> logger)
        {
            _layoutClient = layoutClient;
            _serializer = serializer;
            _logger = logger;
        }
        
        // Inject Sitecore rendering middleware for this controller action
        // (enables model binding to Sitecore objects such as Route,
        // and causes requests to the Sitecore Layout Service for controller actions)
        [UseMvpSiteRendering]
        public async Task<IActionResult> Index(LayoutViewModel model)
        {
            IActionResult result = null;
            ISitecoreRenderingContext request = HttpContext.GetSitecoreRenderingContext();
            
            // request.Response.Content.Sitecore.Route.Placeholders

            var req = request.Response.Content.Sitecore.Route.Placeholders.TryGetValue("main", out var main);

            Placeholder mainPlaceholder = main as Placeholder;
            foreach (Component component in mainPlaceholder)
            {
                var fields = component.Fields;
    
                if (fields.TryGetValue("HeroImage", out var fieldReader))
                {
                    var heroImage = fieldReader.Read<ImageField>(); 
                }
                
                if (fields.TryGetValue("HeroTitle", out var fieldReaderHeroTitle))
                {
                    var heroTitle = fieldReaderHeroTitle.Read<TextField>();
                }
            }

            var sitecoreLayoutRequest = new SitecoreLayoutRequest
            {
                { "sc_site", "mvp-site" },
                { "sc_apikey", "{E2F3D43E-B1FD-495E-B4B1-84579892422A}" },
                { "item", "/About" },
                { "sc_lang", "en" }
            };
            var test = await _layoutClient.Request(sitecoreLayoutRequest);
            
            test.Content.Sitecore.Route.Placeholders.TryGetValue("main", out var mainAbout);

            Placeholder mainPlaceholderAbout = mainAbout as Placeholder;
            foreach (Component component in mainPlaceholderAbout)
            {
                var fields = component.Fields;
                
                

    
                if (fields.TryGetValue("HeroImage", out var fieldReader))
                {
                    var heroImage = fieldReader.Read<ImageField>(); 
                }
                
                if (fields.TryGetValue("HeroTitle", out var fieldReaderHeroTitle))
                {
                    var heroTitle = fieldReaderHeroTitle.Read<TextField>();
                }
            }
            
            // Console.WriteLine(json);
            
            var jsonSettings = new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                ContractResolver = CustomDataContractResolver.Instance,
                Converters = new List<JsonConverter> {
                    new NewtonsoftFieldReaderJsonConverter()
                }
            };
            
            var json = JsonConvert.SerializeObject(test.Content, jsonSettings);
            return Content(json, "application/json");

            // var test = Sitecore.LayoutService.Client.DefaultLayoutClient
            
            if (request.Response?.HasErrors ?? false)
            {
                foreach (SitecoreLayoutServiceClientException error in request.Response.Errors)
                {
                    switch (error)
                    {
                        default:
                            _logger.LogError(error, error.Message);
                            throw error;
                    }
                }
            }
            else if (!(HttpContext.User.Identity?.IsAuthenticated ?? false) && IsSecurePage(request) && !(request.Response?.Content?.Sitecore?.Context?.IsEditing ?? false))
            {
                AuthenticationProperties properties = new()
                {
                    RedirectUri = HttpContext.Request.GetEncodedUrl()
                };

                result = Challenge(properties, OktaDefaults.MvcAuthenticationScheme);
            }
            else
            {
                result = View(model);
            }

            return result;
        }

        private static bool IsSecurePage(ISitecoreRenderingContext request)
        {
            bool result = false;
            if (request.Response?.Content?.Sitecore?.Route?.Fields.TryGetValue("RequiresAuthentication", out IFieldReader requiresAuthFieldReader) ?? false)
            {
                result = requiresAuthFieldReader.Read<CheckboxField>().Value;
            }
            
            return result;
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new LayoutViewModel
            {
                MenuTitle = new TextField("Error")
            });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Healthz()
        {
            // TODO: Do we want to add logic here to confirm connectivity with SC etc?
            return Ok("Healthy");
        }
    }
}