namespace Habitat.Server.Data
{
    public class ConfigSettings
    {
        public int? LogLevel { get; set; }
        public string LogFileTemplate { get; set; }
        public string DataDirectory { get; set; }
    }
}