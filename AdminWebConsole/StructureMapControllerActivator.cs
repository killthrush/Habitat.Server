using System;
using System.Web.Mvc;
using System.Web.Routing;
using StructureMap;

namespace Habitat.Server.AdminWebConsole
{
    /// <summary>
    /// A class responsible for creating controller instances using StructureMap
    /// </summary>
    public class StructureMapControllerActivator : IControllerActivator
    {
        /// <summary>
        /// The StructureMap container that will create the controllers
        /// </summary>
        private readonly IContainer _container;

        /// <summary>
        /// Creates a controller of the appropriate type based on the incoming request
        /// </summary>
        /// <param name="requestContext">Context of the MVC request - contains route info, etc</param>
        /// <param name="controllerType">The type of controller being requested</param>
        /// <returns>The controller instance, or null if the type could not be cast as a Controller</returns>
        public IController Create(RequestContext requestContext, Type controllerType)
        {
            return _container.GetInstance(controllerType) as Controller;
        }

        /// <summary>
        /// Creates an instance of StructureMapControllerActivator
        /// </summary>
        /// <param name="container">The StructureMap container that will create the controllers</param>
        public StructureMapControllerActivator(IContainer container)
        {
            _container = container;
        }
    }
}