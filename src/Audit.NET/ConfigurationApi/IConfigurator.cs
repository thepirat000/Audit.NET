using System;
using Audit.Core.Providers;

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
        /// Globally include the full stack trace in the audit events.
        /// </summary>
        IConfigurator IncludeStackTrace(bool includeStackTrace = true);

        /// <summary>
        /// Globally include the activity trace in the audit events.
        /// </summary>
        IConfigurator IncludeActivityTrace(bool includeActivityTrace = true);
        /// <summary>
        /// Indicates whether each audit scope should create and start a new Distributed Tracing Activity.
        /// </summary>
        IConfigurator StartActivityTrace(bool startActivityTrace = true);

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

#if NET462 || NET472
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
        /// Use a deferred factory to resolve the data provider for each audit event. 
        /// The factory will be called for each individual Audit Event to be saved.
        /// </summary>
        /// <param name="dataProviderFactory">The data provider factory to use. A delegate that is invoked to instantiate a Data Provider based on the Audit Event information</param>
        ICreationPolicyConfigurator UseDeferredFactory(Func<AuditEvent, AuditDataProvider> dataProviderFactory);

        /// <summary>
        /// Use a lazy initializer to create the data provider. 
        /// The factory method is invoked the first time it's needed and only once.
        /// </summary>
        /// <param name="dataProviderInitializer">The data provider initializer to use. A delegate that is invoked only once to instantiate a Data Provider</param>
        ICreationPolicyConfigurator UseLazyFactory(Func<AuditDataProvider> dataProviderInitializer);

        /// <summary>
        /// Use a conditional data provider wrapper that facilitates the configuration of data providers based on the audit event information.
        /// </summary>
        /// <param name="config">The conditional data provider configuration</param>
        ICreationPolicyConfigurator UseConditional(Action<IConditionalDataProviderConfigurator> config);

        /// <summary>
        /// Shortcut for UseCustomProvider, to use a custom provider instance for the event output.
        /// </summary>
        ICreationPolicyConfigurator Use(AuditDataProvider provider);

        /// <summary>
        /// Store the events in memory in a thread-safe list. Useful for testing purposes.
        /// </summary>
        ICreationPolicyConfigurator UseInMemoryProvider();

        /// <summary>
        /// Store the events in memory in a thread-safe list. Useful for testing purposes. Returns the created InMemoryDataProvider instance as an out parameter.
        /// </summary>
        /// <param name="dataProvider">The created InMemoryDataProvider instance</param>
        ICreationPolicyConfigurator UseInMemoryProvider(out InMemoryDataProvider dataProvider);

        /// <summary>
        /// Store the events in memory in a thread-safe BlockingCollection. Useful for scenarios where the events need to be consumed by another thread.
        /// </summary>
        ICreationPolicyConfigurator UseInMemoryBlockingCollectionProvider(Action<IBlockingCollectionProviderConfigurator> config);

        /// <summary>
        /// Store the events in memory in a thread-safe BlockingCollection. Useful for scenarios where the events need to be consumed by another thread.
        /// </summary>
        ICreationPolicyConfigurator UseInMemoryBlockingCollectionProvider();

        /// <summary>
        /// Record audit events as OpenTelemetry-compatible <see cref="System.Diagnostics.Activity"/> spans.
        /// </summary>
        ICreationPolicyConfigurator UseActivityProvider(Action<IActivityProviderConfigurator> config);
    }
}