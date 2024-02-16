using System;

namespace Audit.Core.ConfigurationApi
{
    public class EventLogProviderConfigurator : IEventLogProviderConfigurator
    {
        internal Setting<string> _logName = "Application";
        internal Setting<string> _sourcePath = "Application";
        internal Setting<string> _machineName = ".";
        internal Func<AuditEvent, string> _messageBuilder;

        public IEventLogProviderConfigurator LogName(string logName)
        {
            _logName = logName;
            return this;
        }

        public IEventLogProviderConfigurator LogName(Func<AuditEvent, string> logName)
        {
            _logName = logName;
            return this;
        }

        public IEventLogProviderConfigurator MachineName(string machineName)
        {
            _machineName = machineName;
            return this;
        }

        public IEventLogProviderConfigurator MachineName(Func<AuditEvent, string> machineName)
        {
            _machineName = machineName;
            return this;
        }

        public IEventLogProviderConfigurator SourcePath(string sourcePath)
        {
            _sourcePath = sourcePath;
            return this;
        }

        public IEventLogProviderConfigurator SourcePath(Func<AuditEvent, string> sourcePath)
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
