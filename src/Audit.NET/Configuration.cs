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
        /// Gets or Sets the System Clock implementation. By default DateTime.UtcNow is used to get the current date and time.
        /// </summary>
        public static ISystemClock SystemClock { get; set; }
        /// <summary>
        /// Gets or Sets the Default creation policy.
        /// </summary>
        public static EventCreationPolicy CreationPolicy { get; set; }
        /// <summary>
        /// Gets or Sets the Default data provider factory.
        /// </summary>
        public static Func<AuditDataProvider> DataProviderFactory { get; set; }
        /// <summary>
        /// Gets or Sets the Default data provider instance.
        /// </summary>
        public static AuditDataProvider DataProvider { get { return DataProviderFactory?.Invoke(); } set { DataProviderFactory = () => value; } }
        /// <summary>
        /// Gets or Sets a value that indicates if the logged Type Names should include the namespace. Default is false.
        /// </summary>
        public static bool IncludeTypeNamespaces { get; set; }
        /// <summary>
        /// Gets or Sets the Default audit scope factory.
        /// </summary>
        public static IAuditScopeFactory AuditScopeFactory
        {
            get
            {
                return _auditScopeFactory;
            }
            set
            {
                _auditScopeFactory = value ?? new AuditScopeFactory();
            }
        }
        /// <summary>
        /// Global switch to disable audit logging. Default is false.
        /// </summary>
        public static bool AuditDisabled { get; set; }
        /// <summary>
        /// Global json serializer settings for serializing the audit event on the data providers or by calling the ToJson() method on the AuditEvent.
        /// </summary>
        public static JsonSerializerSettings JsonSettings { get; set; }

        // Custom actions
        internal static Dictionary<ActionType, List<Action<AuditScope>>> AuditScopeActions { get; private set; }

        internal static object Locker = new object();

        private static IAuditScopeFactory _auditScopeFactory;

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
            SystemClock = new DefaultSystemClock();
            ResetCustomActions();
            _auditScopeFactory = new AuditScopeFactory();
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
        /// Attaches a global action to be performed on the audit scope before the audit event is saved.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public static void AddOnSavingAction(Action<AuditScope> action)
        {
            lock (Locker)
            {
                AuditScopeActions[ActionType.OnEventSaving].Add(action);
            }
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope right after it is created and before any saving.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public static void AddOnCreatedAction(Action<AuditScope> action)
        {
            lock (Locker)
            {
                AuditScopeActions[ActionType.OnScopeCreated].Add(action);
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
        /// Removes all the attached actions for the given action type.
        /// </summary>
        public static void ResetCustomActions(ActionType actionType)
        {
            lock (Locker)
            {
                AuditScopeActions[actionType].Clear();
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
