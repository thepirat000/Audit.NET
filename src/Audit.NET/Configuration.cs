using System;
using System.Collections.Generic;
using Audit.Core.Providers;
using Audit.Core.ConfigurationApi;
using System.Linq;
using Newtonsoft.Json;

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
        /// <summary>
        /// Global switch to disable audit logging. Default is false.
        /// </summary>
        public static bool AuditDisabled { get; set; }
        /// <summary>
        /// Global json serializer settings for serializing the audit event on the data providers or by calling the ToJson() method on the AuditEvent.
        /// </summary>
        public static JsonSerializerSettings JsonSettings { get; set; }

        internal static Dictionary<ActionType, List<Action<AuditScope>>> AuditScopeActions { get; private set; }

        internal static object Locker = new object();

        static Configuration()
        {
            AuditDisabled = false;
            DataProvider = new FileDataProvider();
            CreationPolicy = EventCreationPolicy.InsertOnEnd;
            JsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
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
            lock (Locker)
            {
                AuditScopeActions[when].Add(action);
            }
        }
        /// <summary>
        /// Resets the audit scope handlers. Removes all the attached actions for the Audit Scopes.
        /// </summary>
        public static void ResetCustomActions()
        {
            lock (Locker)
            {
                AuditScopeActions = new Dictionary<ActionType, List<Action<AuditScope>>>()
                {
                    {ActionType.OnScopeCreated, new List<Action<AuditScope>>()},
                    {ActionType.OnEventSaving, new List<Action<AuditScope>>()},
                    {ActionType.OnEventSaved, new List<Action<AuditScope>>()}
                };
            }
        }
        /// <summary>
        /// Invokes the scope custom actions.
        /// </summary>
        internal static void InvokeScopeCustomActions(ActionType type, AuditScope auditScope)
        {
            List<Action<AuditScope>> actions;
            lock (Locker)
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
