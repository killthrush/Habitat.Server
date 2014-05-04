using System.Collections.Generic;
using Habitat.Core;

namespace Habitat.Server.AdminWebConsole.Controllers
{
    /// <summary>
    /// Contains the results of an Export operation, one which returns all available data.
    /// </summary>
    public class ExportResult
    {
        /// <summary>
        /// A list of all the available components
        /// </summary>
        public IEnumerable<ConfigRoot> ConfiguredComponents { get; set; }
    }
}