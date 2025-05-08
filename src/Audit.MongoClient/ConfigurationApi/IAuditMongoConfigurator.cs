using System;
using Audit.Core;
using MongoDB.Driver.Core.Events;

namespace Audit.MongoClient.ConfigurationApi
{
    public interface IAuditMongoConfigurator
    {
        /// <summary>
        /// Specifies a predicate to determine whether the audit event should include the server reply. The reply is not included by default.
        /// </summary>
        /// <param name="includeReplyPredicate">A function of the Command Succeeded Event to determine whether to include the the server reply in the audit event</param>
        IAuditMongoConfigurator IncludeReply(Func<CommandSucceededEvent, bool> includeReplyPredicate);

        /// <summary>
        /// Specifies whether the audit event should include the server reply. The reply is not included by default.
        /// </summary>
        /// <param name="include">True to include the server reply, false otherwise</param>
        IAuditMongoConfigurator IncludeReply(bool include = true);

        /// <summary>
        /// Specifies a predicate to determine the event type name on the audit output.
        /// </summary>
        /// <param name="eventTypeNamePredicate">A function of the Command Start Event to determine the event type name. The following placeholders can be used as part of the string: 
        /// - {command}: replaced with the command name.
        /// </param>
        IAuditMongoConfigurator EventType(Func<CommandStartedEvent, string> eventTypeNamePredicate);
        /// <summary>
        /// Specifies the event type name to use in the audit output.
        /// </summary>
        /// <param name="eventTypeName">The event type name to use. The following placeholders can be used as part of the string: 
        /// - {command}: replaced with the command name.
        /// </param>
        IAuditMongoConfigurator EventType(string eventTypeName);

        /// <summary>
        /// Sets a filter function to determine the events to log as a function of the command. By default all commands are logged.
        /// </summary>
        IAuditMongoConfigurator CommandFilter(Func<CommandStartedEvent, bool> commandFilter);

        /// <summary>
        /// Specifies the event creation policy to use for this interception. Default is NULL to use the globally configured creation policy.
        /// </summary>
        /// <param name="eventCreationPolicy">The creation policy to use</param>
        IAuditMongoConfigurator CreationPolicy(EventCreationPolicy eventCreationPolicy);

        /// <summary>
        /// Specifies the audit data provider to use. Default is NULL to use the globally configured data provider.
        /// </summary>
        IAuditMongoConfigurator AuditDataProvider(IAuditDataProvider auditDataProvider);

        /// <summary>
        /// Specifies the Audit Scope factory to use. Default is NULL to use the default AuditScopeFactory.
        /// </summary>
        IAuditMongoConfigurator AuditScopeFactory(IAuditScopeFactory auditScopeFactory);
    }
}