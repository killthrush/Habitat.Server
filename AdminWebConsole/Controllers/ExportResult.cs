using System.Collections.Generic;
using ProTeck.Config.Dto.V1;

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