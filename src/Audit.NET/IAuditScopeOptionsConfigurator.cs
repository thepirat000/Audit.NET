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
        /// Sets the data provider factory to use within this scope.
        /// </summary>
        IAuditScopeOptionsConfigurator DataProvider(Func<AuditDataProvider> dataProviderFactory);
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
    }

}
