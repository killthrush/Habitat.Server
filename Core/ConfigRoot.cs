using System;

namespace Habitat.Core
{
    /// <summary>
    /// The root of the configuration data tree for a given Component (application, service, etc)
    /// </summary>
    public class ConfigRoot
    {
        /// <summary>
        /// The name of the component, e.g. "StorageService" or "Environment"
        /// </summary>
        public string ComponentName { get; set; }

        /// <summary>
        /// The date that this configuration was last modified, i.e. its "age"
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// The actual config data, in tree format
        /// </summary>
        public ConfigNode Data { get; set; }
    }
}
