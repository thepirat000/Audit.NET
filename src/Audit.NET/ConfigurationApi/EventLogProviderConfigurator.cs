namespace Audit.Core.ConfigurationApi
{
    public class EventLogProviderConfigurator : IEventLogProviderConfigurator
    {
        internal string _logName = "Application";
        internal string _sourcePath = "Application";
        internal string _machineName = ".";
        public IEventLogProviderConfigurator LogName(string logName)
        {
            _logName = logName;
            return this;
        }

        public IEventLogProviderConfigurator MachineName(string machineName)
        {
            _machineName = machineName;
            return this;
        }

        public IEventLogProviderConfigurator SourcePath(string sourcePath)
        {
            _sourcePath = sourcePath;
            return this;
        }
    }
}
