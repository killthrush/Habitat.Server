using System.Collections.Generic;

namespace Habitat.Server.AdminWebConsole.Controllers
{
    /// <summary>
    /// Contains the results of an Import operation, one which attempts to import config data from a prior export
    /// </summary>
    public class ImportResult
    {
        /// <summary>
        /// For each item that was imported, this list will contain an entry
        /// </summary>
        public IEnumerable<string> ImportSuccesses { get; set; }

        /// <summary>
        /// For each item that failed to be imported, this list will contain an entry
        /// </summary>
        public IEnumerable<string> ImportWarnings { get; set; }
    }
}