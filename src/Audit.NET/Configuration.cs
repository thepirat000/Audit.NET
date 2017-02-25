using System;
using System.Collections.Generic;
using Audit.Core.Providers;
using Audit.Core.ConfigurationApi;
using System.Linq;

namespace Audit.Core
{
    /// <summary>
    /// Global configuration for Audit.NET 
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Gets or Sets the Default creation policy.
        /// </summary>
        public static EventCreationPolicy CreationPolicy { get; set; }
        /// <summary>
        /// Gets or Sets the Default data provider.
        /// </summary>
        public static AuditDataProvider DataProvider { get; set; }

        internal static Dictionary<ActionType, List<Action<AuditScope>>> AuditScopeActions { get; private set; }

        static Configuration()
        {
            DataProvider = new FileDataProvider();
            CreationPolicy = EventCreationPolicy.InsertOnEnd;
            ResetCustomActions();
        }

        /// <summary>
        /// Configure Audit by using Fluent Configuration API.
        /// </summary>
        public static IConfigurator Setup()
        {
            return new Configurator();
        }

        /// <summary>
        /// Attaches an action to be performed globally on any AuditScope.
        /// </summary>
        /// <param name="when">To indicate when the action should be performed.</param>
        /// <param name="action">The action to perform.</param>
        public static void AddCustomAction(ActionType when, Action<AuditScope> action)
        {
            lock (AuditScopeActions)
            {
                AuditScopeActions[when].Add(action);
            }
        }
        /// <summary>
        /// Resets the audit scope handlers. Removes all the attached actions for the Audit Scopes.
        /// </summary>
        public static void ResetCustomActions()
        {
            AuditScopeActions = new Dictionary<ActionType, List<Action<AuditScope>>>()
            {
                {ActionType.OnScopeCreated, new List<Action<AuditScope>>()},
                {ActionType.OnEventSaving, new List<Action<AuditScope>>()},
            };
        }
        /// <summary>
        /// Invokes the scope custom actions.
        /// </summary>
        internal static void InvokeScopeCustomActions(ActionType type, AuditScope auditScope)
        {
            var actions = Enumerable.Empty<Action<AuditScope>>();
            lock (AuditScopeActions)
            {
                actions = AuditScopeActions[type].ToList();
            }
            foreach (var action in actions)
            {
                action.Invoke(auditScope);
            }
        }
    }
}
