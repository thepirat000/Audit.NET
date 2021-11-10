using System;
using System.Threading.Tasks;

namespace Audit.Core.ConfigurationApi
{
    /// <summary>
    /// Provides a configuration for a DynamicAsyncDataProvider
    /// </summary>
    public interface IDynamicAsyncDataProviderConfigurator
    {
        /// <summary>
        /// Attaches an asynchronous function to be executed by the InsertEvent method.
        /// </summary>
        /// <param name="insertFunction">The insert function.</param>
        IDynamicAsyncDataProviderConfigurator OnInsert(Func<AuditEvent, Task<object>> insertFunction);
        /// <summary>
        /// Attaches an asynchronous action to be executed by the InsertEvent method that will return a random Guid as the event id.
        /// </summary>
        /// <param name="insertAction">The insert action.</param>
        IDynamicAsyncDataProviderConfigurator OnInsert(Func<AuditEvent, Task> insertAction);
        /// <summary>
        /// Attaches an asynchronous action to be executed by the ReplaceEvent method.
        /// </summary>
        /// <param name="replaceAction">The replace action.</param>
        IDynamicAsyncDataProviderConfigurator OnReplace(Func<object, AuditEvent, Task> replaceAction);
        /// <summary>
        /// Attaches an asynchronous action to be executed by the InsertEvent and the ReplaceEvent methods.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="insertReplaceAction">The action to be executed by the InsertEvent and the ReplaceEvent methods.</param>
        IDynamicAsyncDataProviderConfigurator OnInsertAndReplace(Func<AuditEvent, Task> insertReplaceAction);
        /// <summary>
        /// Attaches an asynchronous action to be executed by the InsertEvent and the ReplaceEvent methods, the first parameter (event id) will be NULL in case of insert.
        /// The InsertEvent will generate and return a random Guid as the event id.
        /// </summary>
        /// <param name="insertReplaceAction">The action to be executed by the InsertEvent and the ReplaceEvent methods.</param>
        IDynamicAsyncDataProviderConfigurator OnInsertAndReplace(Func<object, AuditEvent, Task> insertReplaceAction);
    }
}