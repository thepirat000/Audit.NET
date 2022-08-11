using System;
using System.Collections.Generic;
using Audit.Core.Providers;
using Audit.Core.ConfigurationApi;
using System.Linq;
using System.Threading.Tasks;
#if IS_NK_JSON
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
#else
using System.Text.Json;
using System.Text.Json.Serialization;
#endif

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
#if IS_NK_JSON
        public static JsonSerializerSettings JsonSettings { get; set; }
#else
        public static JsonSerializerOptions JsonSettings { get; set; }
#endif
        // Custom actions
        internal static Dictionary<ActionType, List<Func<AuditScope, Task>>> AuditScopeActions { get; private set; }

        internal static object Locker = new object();

        private static IAuditScopeFactory _auditScopeFactory;

        /// <summary>
        /// Gets or sets the json adapter that controls the JSON serialization mechanism.
        /// </summary>
        public static IJsonAdapter JsonAdapter { get; set; } = new JsonAdapter();

        static Configuration()
        {
            AuditDisabled = false;
            DataProvider = new FileDataProvider();
            CreationPolicy = EventCreationPolicy.InsertOnEnd;
#if IS_NK_JSON
            JsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
#else
            JsonSettings = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = null,
#if NET6_0_OR_GREATER
                ReferenceHandler = ReferenceHandler.IgnoreCycles
#endif
            };
#endif
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
                AuditScopeActions[when].Add(scope =>
                {
                    action.Invoke(scope);
                    return Task.Delay(0);
                });
            }
        }

        /// <summary>
        /// Attaches an asynchronous action to be performed globally on any AuditScope.
        /// </summary>
        /// <param name="when">To indicate when the action should be performed.</param>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddCustomAction(ActionType when, Func<AuditScope, Task> asyncAction)
        {
            lock (Locker)
            {
                AuditScopeActions[when].Add(async scope =>
                {
                    await asyncAction.Invoke(scope);
                });
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
                AuditScopeActions[ActionType.OnEventSaving].Add(scope =>
                {
                    action.Invoke(scope);
                    return Task.Delay(0);
                });
            }
        }

        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope before the audit event is saved.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddOnSavingAction(Func<AuditScope, Task> asyncAction)
        {
            lock (Locker)
            {
                AuditScopeActions[ActionType.OnEventSaving].Add(async scope =>
                {
                    await asyncAction.Invoke(scope);
                });
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
                AuditScopeActions[ActionType.OnScopeCreated].Add(scope => 
                {
                    action.Invoke(scope);
                    return Task.Delay(0);
                });
            }
        }

        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope right after it is created and before any saving.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddOnCreatedAction(Func<AuditScope, Task> asyncAction)
        {
            lock (Locker)
            {
                AuditScopeActions[ActionType.OnScopeCreated].Add(async scope =>
                {
                    await asyncAction.Invoke(scope);
                });
            }
        }

        /// <summary>
        /// Resets the audit scope handlers. Removes all the attached actions for the Audit Scopes.
        /// </summary>
        public static void ResetCustomActions()
        {
            lock (Locker)
            {
                AuditScopeActions = new Dictionary<ActionType, List<Func<AuditScope, Task>>>()
                {
                    {ActionType.OnScopeCreated, new List<Func<AuditScope, Task>>()},
                    {ActionType.OnEventSaving, new List<Func<AuditScope, Task>>()},
                    {ActionType.OnEventSaved, new List<Func<AuditScope, Task>>()}
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
        /// Synchronously invokes the scope custom actions.
        /// </summary>
        internal static void InvokeScopeCustomActions(ActionType type, AuditScope auditScope)
        {
            List<Func<AuditScope, Task>> actions;
            lock (Locker)
            {
                actions = AuditScopeActions[type].ToList();
            }
            foreach (var action in actions)
            {
                action.Invoke(auditScope).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Asynchronously invokes the scope custom actions.
        /// </summary>
        internal static async Task InvokeScopeCustomActionsAsync(ActionType type, AuditScope auditScope)
        {
            List<Func<AuditScope, Task>> actions;
            lock (Locker)
            {
                actions = AuditScopeActions[type].ToList();
            }
            foreach (var action in actions)
            {
                await action.Invoke(auditScope);
            }
        }

    }
}
