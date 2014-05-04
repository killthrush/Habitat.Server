using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using StructureMap;

namespace Habitat.Server.AdminWebConsole
{
    /// <summary>
    /// Main application for the Config Web Console
    /// </summary>
    public class ConfigConsoleApplication : HttpApplication
    {
        /// <summary>
        /// URL to the Habitat Server.  It is assumed that the data service is available at "configserver" in any environment
        /// </summary>
        private const string ConfigServiceUrl = "http://configserver/Habitat.Server.Data/";

        /// <summary>
        /// Method that runs when the application starts up
        /// </summary>
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
            if (ConfigurationManager.AppSettings["UseRouteDebugger"] != null)
            {
                RouteDebug.RouteDebugger.RewriteRoutesForTesting(RouteTable.Routes);  
            }

            var container = new Container(new DependencyRegistry(ConfigServiceUrl));
            DependencyResolver.SetResolver(new StructureMapResolver(container));

            // Turn off "powered by MVC" headers for security reasons
            MvcHandler.DisableMvcResponseHeader = true;
        }

        /// <summary>
        /// Wires up all the global filters needed for MVC
        /// </summary>
        /// <param name="filters"></param>
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        /// <summary>
        /// Wires up all the dynamic routes needed for MVC
        /// </summary>
        /// <param name="routes">The collection of routes to register</param>
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute("Default", "{controller}/{action}/{componentName}", new { controller = "Admin", action = "Index", componentName = UrlParameter.Optional } );
            routes.MapRoute("Copy Component", "{controller}/CopyComponent/{existingComponentName}/{newComponentName}", new { controller = "Admin", action = "CopyComponent" });
            routes.MapRoute("Swap Component", "{controller}/SwapComponent/{firstComponentName}/{secondComponentName}", new { controller = "Admin", action = "SwapComponent" });
        }
    }
}
