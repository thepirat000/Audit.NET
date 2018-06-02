using Newtonsoft.Json;
using System;

namespace Audit.Core.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for Audit mechanism.
    /// </summary>
    public interface IConfigurator
    {
        /// <summary>
        /// Globally disable the audit logs.
        /// </summary>
        /// <param name="auditDisabled">A boolean value indicating whether the audit is globally disabled.</param>
        /// <returns></returns>
        IConfigurator AuditDisabled(bool auditDisabled);
        /// <summary>
        /// Use a null provider. No audit events will be saved. Useful for testing purposes or to disable the audit logs.
        /// </summary>
        ICreationPolicyConfigurator UseNullProvider();
        /// <summary>
        /// Use a dynamic custom provider for the event output.
        /// </summary>
        ICreationPolicyConfigurator UseDynamicProvider(Action<IDynamicDataProviderConfigurator> config);
        /// <summary>
        /// Use a dynamic asynchronous custom provider for the event output.
        /// </summary>
        ICreationPolicyConfigurator UseDynamicAsyncProvider(Action<IDynamicAsyncDataProviderConfigurator> config);
        /// <summary>
        /// Store the events in files.
        /// </summary>
        /// <param name="config">The file log provider configuration.</param>
        ICreationPolicyConfigurator UseFileLogProvider(Action<IFileLogProviderConfigurator> config);
        /// <summary>
        /// Store the events in files.
        /// </summary>
        /// <param name="directoryPath">Specifies the directory where to store the audit log files.</param>
        /// <param name="filenamePrefix">Specifies the filename prefix to use in the audit log files.</param>
        /// <param name="directoryPathBuilder">Specifies the directory builder to get the path where to store the audit log files. If this setting is provided, directoryPath setting will be ignored.</param>
        /// <param name="filenameBuilder">Specifies the filename builder to get the filename to store the audit log for an event.</param>
        /// <param name="jsonSettings">Specifies the JSON settings to use to serialize the audit events.</param>
        ICreationPolicyConfigurator UseFileLogProvider(string directoryPath = "", string filenamePrefix = "",
            Func<AuditEvent, string> directoryPathBuilder = null, Func<AuditEvent, string> filenameBuilder = null,
            JsonSerializerSettings jsonSettings = null);
#if NET45
        /// <summary>
        /// Store the events in the windows Event Log.
        /// </summary>
        /// <param name="logName">The windows event log name to use</param>
        /// <param name="sourcePath">The source path to use</param>
        /// <param name="machineName">The name of the machine where the event logs will be save. Default is "." (local machine)</param>
        /// <param name="messageBuilder">A function that takes an AuditEvent and returns the message to log. Default is NULL to log the event JSON representation.</param>
        ICreationPolicyConfigurator UseEventLogProvider(string logName = "Application", string sourcePath = "Application", string machineName = ".", Func<AuditEvent, string> messageBuilder = null);
        /// <summary>
        /// Store the events in the windows Event Log.
        /// </summary>
        /// <param name="config">The windows event log configuration</param>
        ICreationPolicyConfigurator UseEventLogProvider(Action<IEventLogProviderConfigurator> config);
#endif
        /// <summary>
        /// Use a custom provider for the event output.
        /// </summary>
        ICreationPolicyConfigurator UseCustomProvider(AuditDataProvider provider);
    }
}