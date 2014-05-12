using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Habitat.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NLog;
using NLog.Config;
using NLog.Interface;
using NLog.Targets;
using StructureMap;

namespace Habitat.Server.Data.Tests
{
    [TestClass]
    public class DependencyRegistryTests
    {
        [TestMethod]
        public void Ensure_that_durable_memory_repository_can_be_obtained()
        {
            var configSettings = new ConfigSettings();
            configSettings.DataDirectory = "foobar";
            var registry = new DependencyRegistry(configSettings);
            var container = new Container(registry);
            var instance = container.GetInstance<IRepository<IJsonEntity<ConfigRoot>>>();

            Assert.IsNotNull(instance);
            Assert.IsTrue(instance is DurableMemoryRepository<ConfigRoot>);

            Assert.AreEqual(configSettings.DataDirectory, (instance as DurableMemoryRepository<ConfigRoot>).Path);
        }

        [TestMethod]
        public void Ensure_that_logger_can_be_obtained()
        {
            var configSettings = new ConfigSettings();
            configSettings.LogLevel = LogLevel.Error.Ordinal;
            configSettings.LogFileTemplate = "foobar";

            var registry = new DependencyRegistry(configSettings);
            var container = new Container(registry);

            var instance = container.GetInstance<ILogger>();
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance is LoggerAdapter);
            Assert.AreEqual(typeof(DependencyRegistry).Namespace, instance.Name);

            // Should be 1 logfile target
            ReadOnlyCollection<Target> targets = (instance as LoggerAdapter).Factory.Configuration.AllTargets;
            Assert.AreEqual(1, targets.Count);
            FileTarget fileTarget = targets.OfType<FileTarget>().First();
            Assert.IsNotNull(fileTarget);
            Assert.AreEqual("LogFile", fileTarget.Name);

            // Path to the logfile should match config
            Assert.AreEqual(configSettings.LogFileTemplate, fileTarget.FileName.Render(null));

            // Should be a single rule for logging
            IList<LoggingRule> rules = (instance as LoggerAdapter).Factory.Configuration.LoggingRules;
            Assert.AreEqual(1, rules.Count);

            // Should use log level from config
            LoggingRule rule = rules.First();
            Assert.IsNotNull(rule);
            Assert.IsNotNull(rule.Levels);
            Assert.AreEqual(2, rule.Levels.Count);
            Assert.AreEqual(rule.Levels[0].Ordinal, LogLevel.Error.Ordinal);
            Assert.AreEqual(rule.Levels[1].Ordinal, LogLevel.Fatal.Ordinal);
        }

        [TestMethod]
        public void If_log_directory_is_undefined_use_working_directory()
        {
            var configSettings = new ConfigSettings();
            var registry = new DependencyRegistry(configSettings);
            var container = new Container(registry);

            var instance = container.GetInstance<ILogger>();
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance is LoggerAdapter);
            Assert.AreEqual(typeof(DependencyRegistry).Namespace, instance.Name);

            // Should be 1 logfile target
            ReadOnlyCollection<Target> targets = (instance as LoggerAdapter).Factory.Configuration.AllTargets;
            Assert.AreEqual(1, targets.Count);         
            FileTarget fileTarget = targets.OfType<FileTarget>().First();
            Assert.IsNotNull(fileTarget);
            Assert.AreEqual("LogFile", fileTarget.Name);

            // Path to the logfile should be the default
            string expectedLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "default.log");
            Assert.AreEqual(expectedLogPath, fileTarget.FileName.Render(null));
        }

        [TestMethod]
        public void If_log_level_is_undefined_use_info_by_default()
        {
            var configSettings = new ConfigSettings();
            var registry = new DependencyRegistry(configSettings);
            var container = new Container(registry);

            var instance = container.GetInstance<ILogger>();
            Assert.IsNotNull(instance);
            Assert.IsTrue(instance is LoggerAdapter);
            Assert.AreEqual(typeof(DependencyRegistry).Namespace, instance.Name);

            // Should be a single rule for logging
            IList<LoggingRule> rules = (instance as LoggerAdapter).Factory.Configuration.LoggingRules;
            Assert.AreEqual(1, rules.Count);

            // Should default to logging Info and up
            LoggingRule rule = rules.First();
            Assert.IsNotNull(rule);
            Assert.IsNotNull(rule.Levels);
            Assert.AreEqual(4, rule.Levels.Count);
            Assert.AreEqual(rule.Levels[0].Ordinal, LogLevel.Info.Ordinal);
            Assert.AreEqual(rule.Levels[1].Ordinal, LogLevel.Warn.Ordinal);
            Assert.AreEqual(rule.Levels[2].Ordinal, LogLevel.Error.Ordinal);
            Assert.AreEqual(rule.Levels[3].Ordinal, LogLevel.Fatal.Ordinal);
        }

        [TestMethod]
        public void If_data_directory_is_use_working_directory_and_warn()
        {
            var configSettings = new ConfigSettings();
            var registry = new DependencyRegistry(configSettings);
            var container = new Container(registry);
            var instance = container.GetInstance<IRepository<IJsonEntity<ConfigRoot>>>();

            Assert.IsNotNull(instance);
            Assert.IsTrue(instance is DurableMemoryRepository<ConfigRoot>);

            string expectedDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configdata");
            Assert.AreEqual(expectedDataPath, (instance as DurableMemoryRepository<ConfigRoot>).Path);
        }
    }
}
