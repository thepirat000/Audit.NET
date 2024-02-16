using System;

namespace Audit.Core.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for the EventLogDataProvider
    /// </summary>
    public interface IEventLogProviderConfigurator
    {
        /// <summary>
        /// Specifies the EventLog Log Name to use.
        /// </summary>
        /// <param name="logName">The Log Name</param>
        IEventLogProviderConfigurator LogName(string logName);
        /// <summary>
        /// Specifies the function to obtain the Log Name to use.
        /// </summary>
        /// <param name="logName">The Log Name function</param>
        IEventLogProviderConfigurator LogName(Func<AuditEvent, string> logName);
        /// <summary>
        /// Specifies the EventLog Source Path to use.
        /// </summary>
        /// <param name="sourcePath">The Source Path</param>
        IEventLogProviderConfigurator SourcePath(string sourcePath);
        /// <summary>
        /// Specifies the function to obtain the EventLog Source Path to use.
        /// </summary>
        /// <param name="sourcePath">The Source Path function</param>
        IEventLogProviderConfigurator SourcePath(Func<AuditEvent, string> sourcePath);
        /// <summary>
        /// Specifies the name of the machine to write to its EventLog.
        /// </summary>
        /// <param name="machineName">The Log Name</param>
        IEventLogProviderConfigurator MachineName(string machineName);
        /// <summary>
        /// Specifies the function to obtain the Machine Name to use.
        /// </summary>
        IEventLogProviderConfigurator MachineName(Func<AuditEvent, string> machineName);
        /// <summary>
        /// Specifies the function to obtain the message to log. Default is the event JSON representation.
        /// </summary>
        /// <param name="messageBuilder">A function that takes an AuditEvent and returns the message to log</param>
        IEventLogProviderConfigurator MessageBuilder(Func<AuditEvent, string> messageBuilder);
    }
}