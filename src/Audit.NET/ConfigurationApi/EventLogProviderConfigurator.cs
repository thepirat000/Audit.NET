using System;

namespace Audit.Core.ConfigurationApi
{
    public class EventLogProviderConfigurator : IEventLogProviderConfigurator
    {
        internal string _logName = "Application";
        internal string _sourcePath = "Application";
        internal string _machineName = ".";
        internal Func<AuditEvent, string> _messageBuilder;


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

        public IEventLogProviderConfigurator MessageBuilder(Func<AuditEvent, string> messageBuilder)
        {
            _messageBuilder = messageBuilder;
            return this;
        }
    }
}
