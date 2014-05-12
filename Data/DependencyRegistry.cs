using System;
using System.IO;
using System.Linq;
using Habitat.Core;
using NLog;
using NLog.Config;
using NLog.Interface;
using NLog.Targets;
using StructureMap.Configuration.DSL;

namespace Habitat.Server.Data
{
    /// <summary>
    /// Class used with StructureMap to set up DI mappings
    /// </summary>
    public class DependencyRegistry : Registry
    {
        /// <summary>
        /// Constructs an instance of DependencyRegistry
        /// </summary>
        public DependencyRegistry(ConfigSettings configSettings)
        {
            string logFileTemplate = configSettings.LogFileTemplate ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default.log");
            string dataDirectory = configSettings.DataDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configdata");
            LogLevel logLevel = GetMatchingLogLevelOrDefault(configSettings.LogLevel) ?? LogLevel.Info;

            For<IRepository<IJsonEntity<ConfigRoot>>>()
                .Singleton()
                .Use(new DurableMemoryRepository<ConfigRoot>(dataDirectory, new FileSystemFacade()));

            var config = new LoggingConfiguration();
            var fileTarget = new FileTarget();
            fileTarget.Name = "LogFile";
            fileTarget.FileName = logFileTemplate;
            config.AddTarget(fileTarget.Name, fileTarget);

            var loggingRule = new LoggingRule("*", logLevel, fileTarget);
            config.LoggingRules.Add(loggingRule);
            
            LogManager.Configuration = config;

            For<ILogger>()
                .Singleton()
                .Use(l => new LoggerAdapter(LogManager.GetLogger(GetType().Namespace)));

            // Ask StructureMap to always do property injection for certain properties
            // TODO: remove this?
            SetAllProperties(policy => policy.OfType<ILogger>()); 
        }

        private LogLevel GetMatchingLogLevelOrDefault(int? logLevel)
        {
            LogLevel[] logLevels = new[]
                               {
                                   LogLevel.Debug,
                                   LogLevel.Error,
                                   LogLevel.Fatal,
                                   LogLevel.Info,
                                   LogLevel.Off,
                                   LogLevel.Trace,
                                   LogLevel.Warn
                               };
            return logLevels.FirstOrDefault(x => x.Ordinal == logLevel);
        }
    }
}