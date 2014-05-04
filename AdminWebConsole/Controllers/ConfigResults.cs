namespace Habitat.Server.AdminWebConsole.Controllers
{
    /// <summary>
    /// Results package that will be serialized to JSON and sent to the caller from the AdminController
    /// </summary>
    /// <typeparam name="TR">The type of object returned in the config results</typeparam>
    public class ConfigResults<TR>
    {
        /// <summary>
        /// Contains data from the Habitat Server for a requested action.  Null if there's no data or if there's an error.
        /// </summary>
        public TR Data { get; set; }

        /// <summary>
        /// If there was an exception, this will be non-null and will contain the exception message
        /// </summary>
        public string ExceptionMessage { get; set; }
    }
}