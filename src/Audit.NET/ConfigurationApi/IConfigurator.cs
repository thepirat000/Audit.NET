using System;
#if IS_NK_JSON
using Newtonsoft.Json;
#else
using System.Text.Json;
#endif

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
        /// Use a custom JsonAdapter
        /// </summary>
        /// <param name="adapter">The JSON adapter instance.</param>
        IConfigurator JsonAdapter(IJsonAdapter adapter);
        /// <summary>
        /// Use a custom JsonAdapter of type <typeparamref name="T"/> using the parameterless constructor
        /// </summary>
        IConfigurator JsonAdapter<T>() where T : IJsonAdapter;
        /// <summary>
        /// Use a null provider. No audit events will be saved. Useful for testing purposes or to disable the audit logs.
        /// </summary>
        ICreationPolicyConfigurator UseNullProvider();
        /// <summary>
        /// Use a dynamic custom provider for the event output.
        /// </summary>
        /// <param name="config">The fluent configuration of the dynamic provider</param>
        ICreationPolicyConfigurator UseDynamicProvider(Action<IDynamicDataProviderConfigurator> config);
        /// <summary>
        /// Use a dynamic asynchronous custom provider for the event output.
        /// </summary>
        /// <param name="config">The fluent configuration of the async dynamic provider</param>
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
        ICreationPolicyConfigurator UseFileLogProvider(string directoryPath = "", string filenamePrefix = "",
            Func<AuditEvent, string> directoryPathBuilder = null, Func<AuditEvent, string> filenameBuilder = null);

#if NET45 || NET461
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
        /// <param name="provider">The data provider instance to use</param>
        ICreationPolicyConfigurator UseCustomProvider(AuditDataProvider provider);

        /// <summary>
        /// Shortcut for UseDynamicProvider, to use a dynamic custom provider for the event output.
        /// </summary>
        ICreationPolicyConfigurator Use(Action<IDynamicDataProviderConfigurator> config);

        /// <summary>
        /// Use a custom factory to create the data provider. 
        /// The factory is called when an AuditScope is created.
        /// </summary>
        /// <param name="dataProviderFactory">The data provider factory to use. A delegate that is invoked to instantiate an AuditDataProvider</param>
        ICreationPolicyConfigurator UseFactory(Func<AuditDataProvider> dataProviderFactory);

        /// <summary>
        /// Shortcut for UseCustomProvider, to use a custom provider instance for the event output.
        /// </summary>
        ICreationPolicyConfigurator Use(AuditDataProvider provider);

        /// <summary>
        /// Shortcut for UseFactory, to use a custom provider factory for the event output.
        /// </summary>
        /// <param name="dataProviderFactory">The data provider factory to use. A delegate that is invoked to instantiate an AuditDataProvider</param>
        ICreationPolicyConfigurator Use(Func<AuditDataProvider> dataProviderFactory);

        /// <summary>
        /// Store the events in memory in a thread-safe list. Useful for testing purposes.
        /// </summary>
        ICreationPolicyConfigurator UseInMemoryProvider();
    }
}