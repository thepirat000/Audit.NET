using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Audit.Core.Providers;

namespace Audit.Core
{
    /// <summary>
    /// Global configuration for Audit.NET 
    /// </summary>
    public static class AuditConfiguration
    {
        private static Dictionary<ActionType, List<Action<AuditScope>>> _auditScopeActions;

        /// <summary>
        /// Gets or Sets the Default creation policy.
        /// </summary>
        public static EventCreationPolicy CreationPolicy { get; private set; } 
        /// <summary>
        /// Gets the Default data provider.
        /// </summary>
        public static AuditDataProvider DataProvider { get; private set; }

        static AuditConfiguration()
        {
            DataProvider = new FileDataProvider();
            CreationPolicy = EventCreationPolicy.InsertOnEnd;
            ResetCustomActions();
        }
        /// <summary>
        /// Sets the default data provider to use.
        /// </summary>
        /// <param name="dataProvider">The data provider.</param>
        public static void SetDataProvider(AuditDataProvider dataProvider)
        {
            DataProvider = dataProvider;
        }
        /// <summary>
        /// Sets the default creation policy.
        /// </summary>
        /// <param name="creationPolicy">The event creation policy to use.</param>
        public static void SetCreationPolicy(EventCreationPolicy creationPolicy)
        {
            CreationPolicy = creationPolicy;
        }
        /// <summary>
        /// Attaches an action to be performed globally on any AuditScope.
        /// </summary>
        /// <param name="when">To indicate when the action should be performed.</param>
        /// <param name="action">The action to perform.</param>
        public static void AddCustomAction(ActionType when, Action<AuditScope> action)
        {
            _auditScopeActions[when].Add(action);
        }
        /// <summary>
        /// Resets the audit scope handlers. Removes all the attached actions for the Audit Scopes.
        /// </summary>
        public static void ResetCustomActions()
        {
            _auditScopeActions = new Dictionary<ActionType, List<Action<AuditScope>>>()
            {
                {ActionType.OnScopeCreated, new List<Action<AuditScope>>()},
                {ActionType.OnEventSaving, new List<Action<AuditScope>>()},
            };
        }
        internal static void InvokeScopeCustomActions(ActionType type, AuditScope auditScope)
        {
            foreach (var action in _auditScopeActions[type])
            {
                action.Invoke(auditScope);
            }
        }
    }
}
