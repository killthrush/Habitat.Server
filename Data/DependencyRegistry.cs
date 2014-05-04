using System.Configuration;
using Habitat.Core;
using StructureMap.Configuration.DSL;

namespace Habitat.Server.Data
{
    /// <summary>
    /// Class used with StructureMap to set up DI mappings
    /// </summary>
    public class DependencyRegistry : Registry
    {
        private const string LogFilePathTemplate = @"c:\protk\logs\%dProTeck.Config.Data.txt";  // TODO: use a different log folder

        /// <summary>
        /// Constructs an instance of DependencyRegistry
        /// </summary>
        public DependencyRegistry()
        {
            int logLevel;
            if (!int.TryParse(ConfigurationManager.AppSettings["LogLevel"], out logLevel))
            {
                logLevel = 0;
            }

            For<IRepository<IJsonEntity<ConfigRoot>>>()
                .Singleton()
                .Use(new DurableMemoryRepository<ConfigRoot>(@"C:\protk\data\configservice", new FileSystemFacade()));  // TODO: should be configurable

           
            // TODO: use NLog for all logging

/*            For<ILog>()
                .Singleton()
                .Use(l => new FileLog(LogFilePathTemplate, (LogLevel)logLevel));

            // Ask SM to always do property injection for certain properties
            SetAllProperties(policy => policy.OfType<ILog>());*/ 
        }
    }
}