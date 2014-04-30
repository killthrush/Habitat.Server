using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using StructureMap;

namespace Habitat.Server.AdminWebConsole
{
    /// <summary>
    /// Class that hooks StructureMap up to MVC 3's dependency resolver logic.
    /// </summary>
    internal class StructureMapResolver : IDependencyResolver
    {
        /// <summary>
        /// The StructureMap container to use for dependency injection
        /// </summary>
        private readonly IContainer _container;

        /// <summary>
        /// Creates an instance of StructureMapResolver
        /// </summary>
        /// <param name="container">The StructureMap container to use for dependency injection</param>
        public StructureMapResolver(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Resolves singly registered services that support arbitrary object creation.
        /// </summary>
        /// <returns>
        /// The requested service or object.
        /// </returns>
        /// <param name="serviceType">The type of the requested service or object.</param>
        public object GetService(Type serviceType)
        {
            try
            {
                return _container.GetInstance(serviceType);
            }
            catch (StructureMapException)
            {
                return null; // TODO: figure out what to do here.  It's common for MVC to ask for types get a null and then revert back to default implementations.  But we don't want our stuff to work that way.
            }
        }

        /// <summary>
        /// Resolves multiply registered services.
        /// </summary>
        /// <returns>
        /// The requested services.
        /// </returns>
        /// <param name="serviceType">The type of the requested services.</param>
        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                return _container.GetAllInstances(serviceType).Cast<object>();
            }
            catch (StructureMapException)
            {
                return null; // TODO: figure out what to do here.  It's common for MVC to ask for types get a null and then revert back to default implementations.  But we don't want our stuff to work that way.
            }
        }
    }
}