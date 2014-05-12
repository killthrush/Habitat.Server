using System;
using System.Linq;
using System.Text.RegularExpressions;
using Habitat.Core;
using NLog.Interface;
using OpenRasta.Web;
using ProTeck.Core.OpenRasta;
using StructureMap;

namespace Habitat.Server.Data.Handlers
{
    /// <summary>
    /// Handles requests (all verbs) for config data service.
    /// </summary>
    public class ConfigHandler
    {
        private readonly IRequest _request;
        private readonly IResponse _response;

        /// <summary>
        /// StructureMap container to use with the handler
        /// </summary>
        private readonly IContainer _container;

        /// <summary>
        /// Repository to use for storing/retrieving config information
        /// </summary>
        private readonly IRepository<IJsonEntity<ConfigRoot>> _repository;

        /// <summary>
        /// Logger instance to use
        /// </summary>
        private readonly ILogger _log;

        /// <summary>
        /// Constructs an instance of FileResourceHandler
        /// </summary>
        /// <param name="request">Request object injected by OpenRasta</param>
        /// <param name="response">Response object injected by OpenRasta</param>
        /// <param name="container">StructureMap container to use with the handler</param>
        public ConfigHandler(IRequest request, IResponse response, IContainer container)
        {
            _request = request;
            _response = response;
            _container = container;

            _repository = _container.GetInstance<IRepository<IJsonEntity<ConfigRoot>>>();
            _log = _container.GetInstance<ILogger>();
        }

        /// <summary>
        /// Gets list of components that have configuration. Maps to GET /
        /// </summary>
        /// <returns>List of the names of components</returns>
        public OperationResult GetComponentList()
        {
            var componentNames = _repository.Entities.Select(x => x.Contents.ComponentName).OrderBy(x => x).ToList();

            return new OperationResult.OK {ResponseResource = componentNames};
        }

        /// <summary>
        /// Gets configuration data for a particular component. Maps to GET /{componentName}
        /// </summary>
        /// <param name="componentName"></param>
        /// <returns>
        /// 404 Not Found - If component not found
        /// 200 OK - Returns component config in response body
        /// 500 Internal Server Error - if error occurs
        /// </returns>
        public OperationResult Get(string componentName)
        {
            _log.Debug(string.Format("Entering ConfigHandler.Get(). componentName = '{0}'", componentName));

            try
            {
                var configEntity = _repository.Entities.FirstOrDefault(
                    x => x.Contents != null && x.Contents.ComponentName.ToLower() == componentName.ToLower());

                if (configEntity == null)
                {
                    _log.Debug(string.Format("Returning 404 Not Found for resource {0}", componentName));
                    return new OperationResult.NotFound();
                }

                _response.Headers[HttpHelper.LastModifiedHeader] =
                    HttpHelper.FormatDateTime(configEntity.Contents.LastModified);
                _response.Headers.ContentType = MediaType.Json;

                return new OperationResult.OK(configEntity.Contents);
            }
            catch (Exception exception)
            {
                _log.Error(string.Format("Error in Get for component '{0}': {1}", componentName, exception));
                throw;
            }
        }

        /// <summary>
        /// Handles POST for creating new resource.
        /// </summary>
        /// <param name="config">Config root deserialized from JSON in request body</param>
        /// <returns>
        /// 409 Conflict - If config exists for component of same name already in repository
        /// 201 Created - If adding new config succeeds
        /// 500 Internal Server Error - If error occurs
        /// </returns>
        public OperationResult Post(ConfigRoot config)
        {
            _log.Debug("Entering ConfigHandler.Post()");

            try
            {
                IJsonEntity<ConfigRoot> configEntity = _repository.Entities.FirstOrDefault(
                    x => x.Contents.ComponentName.ToLower() == config.ComponentName.ToLower());

                if (configEntity != null)
                {
                    _log.Debug(string.Format("Returning 400 Bad Request for POST to component {0}", config.ComponentName));
                    return new OperationResult.BadRequest();
                }

                if (config.ComponentName == null || Regex.IsMatch(config.ComponentName, @"\W"))
                {
                    _log.Debug(string.Format("Returning 400 Bad Request for POST to component {0}", config.ComponentName));
                    return new OperationResult.BadRequest();
                }

                configEntity = _repository.Create();
                configEntity.Contents = config;

                _repository.Add(configEntity);
                _repository.Save();


                var location = HttpHelper.GetLocation(_request.Uri, config.ComponentName);
                _response.Headers[HttpHelper.LocationHeader] = location.ToString();
                return new OperationResult.Created {ResponseResource = configEntity.Contents};
            }
            catch (Exception exception)
            {
                _log.Error(string.Format("Error in Post for component '{0}': {1}", config != null ? config.ComponentName : null, exception));
                throw;
            }
        }

        /// <summary>
        /// Performs an update on an existing Config resource and returns a copy of the saved data if successful.
        /// </summary>
        /// <param name="componentName">The name of the resource to update.  Not case-sensitive.</param>
        /// <param name="config">The new config data to store - overwrites any exsiting data</param>
        /// <returns>
        /// 200 - If update is successful
        /// 409 - If an attempt is made to change the name of an existing resource and that name change would cause it to overwrite another resource (see remarks)
        /// 404 - If the requested resource does not exist
        /// </returns>
        /// <remarks>
        /// This operation allows resource renaming.  If you change the component name in the config JSON, when the operation completes the resource will have a 
        /// new Location returned in the headers, provided that no other resource exists with the same name.
        /// </remarks>
        public OperationResult Put(string componentName, ConfigRoot config)
        {
            _log.Debug("Entering ConfigHandler.Put()");

            try
            {
                var requestedEntity = _repository.Entities.FirstOrDefault(
                    x => x.Contents.ComponentName.ToUpper() == componentName.ToUpper());

                if (requestedEntity == null)
                {
                    _log.Debug(string.Format("Returning 404 Not Found for resource {0}", componentName));
                    return new OperationResult.NotFound();
                }

                if (config.ComponentName == null || Regex.IsMatch(config.ComponentName, @"\W"))
                {
                    return new OperationResult.BadRequest();
                }

                IJsonEntity<ConfigRoot> existingEntity =
                    _repository.Entities.FirstOrDefault(
                        x => x.Contents.ComponentName.ToUpper() == config.ComponentName.ToUpper());
                if (existingEntity != null && config.ComponentName.ToUpper() != componentName.ToUpper())
                {
                    _log.Debug(
                        string.Format(
                            "Returning 409 Conflict for resource {0} - attempted to change name to existing resource {1}",
                            componentName, config.ComponentName));
                    return new ConflictOperationResult();
                }

                requestedEntity.Contents = config;

                _repository.Update(requestedEntity);
                _repository.Save();

                var location = HttpHelper.GetLocation(_request.Uri, config.ComponentName);
                _response.Headers[HttpHelper.LocationHeader] = location.ToString();
                return new OperationResult.OK {ResponseResource = requestedEntity.Contents};
            }
            catch (Exception exception)
            {
                _log.Error(string.Format("Error in Put for component '{0}': {1}", componentName, exception));
                throw;
            }
        }

        /// <summary>
        /// Deletes configuration data for a particular component. Maps to DELETE /{componentName}
        /// </summary>
        /// <param name="componentName"></param>
        /// <returns>
        /// 404 Not Found - If component does not exist
        /// 204 No Content - If delete succeeds
        /// 500 Internal Server Error - if error occurs
        /// </returns>
        public OperationResult Delete(string componentName)
        {
            _log.Debug(string.Format("Entering ConfigHandler.Delete(). componentName = '{0}'", componentName));

            try
            {
                var configEntity = _repository.Entities.FirstOrDefault(
                    x => x.Contents.ComponentName.ToLower() == componentName.ToLower());

                if (configEntity == null)
                {
                    _log.Debug(string.Format("Returning 404 Not Found for resource {0}", componentName));
                    return new OperationResult.NotFound();
                }

                _repository.Delete(configEntity);
                _repository.Save();

                return new OperationResult.NoContent();
            }
            catch (Exception exception)
            {
                _log.Error(string.Format("Error in Delete for component '{0}': {1}", componentName, exception));
                throw;
            }
        }
    }
}