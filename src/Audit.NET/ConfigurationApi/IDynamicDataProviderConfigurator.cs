using System;

namespace Audit.Core.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for a DynamicDataProvider
    /// </summary>
    public interface IDynamicDataProviderConfigurator
    {
        /// <summary>
        /// Attaches a function to be executed by the InsertEvent method.
        /// </summary>
        /// <param name="insertFunction">The insert function.</param>
        IDynamicDataProviderConfigurator OnInsert(Func<AuditEvent, object> insertFunction);
        /// <summary>
        /// Attaches an action to be executed by the InsertEvent method that will return a random Guid as the event id.
        /// </summary>
        /// <param name="insertAction">The insert action.</param>
        IDynamicDataProviderConfigurator OnInsert(Action<AuditEvent> insertAction);
        /// <summary>
        /// Attaches an action to be executed by the ReplaceEvent method.
        /// </summary>
        /// <param name="replaceAction">The replace action.</param>
        IDynamicDataProviderConfigurator OnReplace(Action<object, AuditEvent> replaceAction);
        /// <summary>
        /// Attaches an action to be executed by the InsertEvent and the ReplaceEvent methods.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="insertReplaceAction">The action to be executed by the InsertEvent and the ReplaceEvent methods.</param>
        IDynamicDataProviderConfigurator OnInsertAndReplace(Action<AuditEvent> insertReplaceAction);
        /// <summary>
        /// Attaches an action to be executed by the InsertEvent and the ReplaceEvent methods, the first parameter (event id) will be NULL in case of insert.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="insertReplaceAction">The action to be executed by the InsertEvent and the ReplaceEvent methods.</param>
        IDynamicDataProviderConfigurator OnInsertAndReplace(Action<object, AuditEvent> insertReplaceAction);
    }
}