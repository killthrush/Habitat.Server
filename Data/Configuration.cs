using System;
using System.Collections.Generic;
using System.Configuration;
using Habitat.Core;
using OpenRasta.Configuration;
using OpenRasta.DI;
using Habitat.Server.Data.Codecs;
using Habitat.Server.Data.Handlers;
using OpenRasta.Web;
using StructureMap;

namespace Habitat.Server.Data
{
    /// <summary>
    /// By OpenRasta convention, this class configures the web application. It sets up routes,
    /// handlers, codecs, and any custom dependency injection. In this case, it also sets up 
    /// StructureMap configuration. StructureMap is not used to resolve internal OpenRasta dependencies
    /// as it's not officially supported, but is used within the handlers and codecs.
    /// </summary>
    public class Configuration : IConfigurationSource
    {
        /// <summary>
        /// StructureMap container to use with this data service
        /// </summary>
        private IContainer _container;

        /// <summary>
        /// Initializes a new instance of the Configuration class.
        /// </summary>
        public Configuration()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Configuration class.
        /// </summary>
        /// <param name="container">StructureMap container to use with this data service</param>
        public Configuration(IContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Configures OpenRasta and StructureMap for the Habitat Server.
        /// </summary>
        public void Configure()
        {
            try
            {
                using (OpenRastaConfiguration.Manual)
                {
                    ResourceSpace.Has.ResourcesOfType<ConfigRoot>()
                        .AtUri("Config/{componentName}").And
                        .AtUri("Config")
                        .HandledBy<ConfigHandler>()
                        .TranscodedBy<ConfigCodec>()
                        .ForMediaType(MediaType.Json);

                    ResourceSpace.Has.ResourcesOfType<List<string>>()
                        .AtUri("Config")
                        .HandledBy<ConfigHandler>()
                        .TranscodedBy<ConfigListCodec>()
                        .ForMediaType(MediaType.Json);
                }


                if (_container == null)
                {
                    ConfigSettings configSettings = new ConfigSettings();
                    configSettings.DataDirectory = ConfigurationManager.AppSettings["DataDirectory"];
                    configSettings.LogFileTemplate = ConfigurationManager.AppSettings["LogFileTemplate"];
               
                    int logLevel;
                    if (int.TryParse(ConfigurationManager.AppSettings["LogLevel"], out logLevel))
                    {
                        configSettings.LogLevel = logLevel;
                    }

                    _container = new Container(new DependencyRegistry(configSettings));
                }

                ResourceSpace.Uses.Resolver.AddDependencyInstance(typeof(IContainer), _container, DependencyLifetime.Singleton);
            }
            catch (Exception exception)
            {
                
            }
        }
    }
}