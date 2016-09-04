namespace Audit.Core.Configuration
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
        /// Specifies the EventLog Source Path to use.
        /// </summary>
        /// <param name="sourcePath">The Source Path</param>
        IEventLogProviderConfigurator SourcePath(string sourcePath);
        /// <summary>
        /// Specifies the name of the machine to write to its EventLog.
        /// </summary>
        /// <param name="machineName">The Log Name</param>
        IEventLogProviderConfigurator MachineName(string machineName);
    }
}