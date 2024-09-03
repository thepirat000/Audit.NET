using System;
using System.Reflection;

namespace Audit.Core
{
    public interface IAuditScopeOptionsConfigurator
    {
        /// <summary>
        /// Sets the string representing the type of the event.
        /// </summary>
        IAuditScopeOptionsConfigurator EventType(string eventType);
        /// <summary>
        /// Sets the target object getter (a getter to the object to track)
        /// </summary>
        IAuditScopeOptionsConfigurator Target(Func<object> targetGetter);
        /// <summary>
        /// Sets the anonymous object that contains additional fields to be merged into the audit event.
        /// </summary>
        IAuditScopeOptionsConfigurator ExtraFields(object extraFields);
        /// <summary>
        /// Sets the event creation policy to use within this scope.
        /// </summary>
        IAuditScopeOptionsConfigurator CreationPolicy(EventCreationPolicy creationPolicy);
        /// <summary>
        /// Sets the data provider to use within this scope.
        /// </summary>
        IAuditScopeOptionsConfigurator DataProvider(AuditDataProvider dataProvider);
        /// <summary>
        /// Use a deferred factory to resolve the data provider for each audit event. The factory will be called for each individual Audit Event to be saved. This is a shortcut to set a DeferredDataProvider.
        /// </summary>
        IAuditScopeOptionsConfigurator DataProviderDeferredFactory(Func<AuditEvent, AuditDataProvider> dataProviderFactory);
        /// <summary>
        /// Sets the data provider factory to use within this scope. The initializer method will be invoked the first time it's needed and only once. This is a shortcut to set a LazyDataProvider.
        /// </summary>
        IAuditScopeOptionsConfigurator DataProviderLazyFactory(Func<AuditDataProvider> dataProviderFactory);
        /// <summary>
        /// To indicate whether this scope should be immediately saved after creation
        /// </summary>
        IAuditScopeOptionsConfigurator IsCreateAndSave(bool isCreateAndSave = true);
        /// <summary>
        /// Sets the initial audit event to use within this scope.
        /// </summary>
        IAuditScopeOptionsConfigurator AuditEvent(AuditEvent auditEvent);
        /// <summary>
        /// Sets the value used to indicate how many frames in the stack should be skipped to determine the calling method
        /// </summary>
        IAuditScopeOptionsConfigurator SkipExtraFrames(int extraFrames);
        /// <summary>
        /// Sets a specific calling method to store on the event.
        /// </summary>
        IAuditScopeOptionsConfigurator CallingMethod(MethodBase method);
        /// <summary>
        /// Sets the value used to indicate whether the audit event's environment should include the full stack trace
        /// </summary>
        IAuditScopeOptionsConfigurator IncludeStackTrace(bool includeStackTrace = true);
        /// <summary>
        /// Sets the value used to indicate whether the audit event's should exclude the environment information
        /// </summary>
        IAuditScopeOptionsConfigurator ExcludeEnvironmentInfo(bool excludeEnvironmentInfo = true);
        /// <summary>
        /// Sets the system clock to use within this scope.
        /// </summary>
        /// <param name="systemClock">The system clock to use</param>
        IAuditScopeOptionsConfigurator SystemClock(ISystemClock systemClock);
        /// <summary>
        /// Adds an item to the custom items collection within the audit scope.
        /// These items are excluded from the audit event output but may be utilized by data providers or custom actions
        /// </summary>
        /// <param name="key">The item key</param>
        /// <param name="value">The item value</param>
        IAuditScopeOptionsConfigurator WithItem(string key, object value);
    }

}
