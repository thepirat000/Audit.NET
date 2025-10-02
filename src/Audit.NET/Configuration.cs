using Audit.Core.ConfigurationApi;
using Audit.Core.Providers;
using Audit.Core.Providers.Wrappers;

using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

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
        /// Sets the Default data provider as a factory method that will be invoked the first time it's needed and only once. This is a shortcut to set a LazyDataProvider.
        /// </summary>
        public static Func<IAuditDataProvider> DataProviderFactory 
        {
            set => DataProvider = new LazyDataProvider(value);
        }

        /// <summary>
        /// Gets or Sets the Default data provider instance.
        /// </summary>
        public static IAuditDataProvider DataProvider { get; set; }

        /// <summary>
        /// Gets the Default data provider instance as the specified type. Returns null if the data provider is not of the given type.
        /// </summary>
        /// <typeparam name="T">The IAuditDataProvider type</typeparam>
        public static T DataProviderAs<T>() where T : class, IAuditDataProvider { return DataProvider as T; } 

        /// <summary>
        /// Gets or Sets a value that indicates if the logged Type Names should include the namespace. Default is false.
        /// </summary>
        public static bool IncludeTypeNamespaces { get; set; }

        /// <summary>
        /// Gets or Sets the value used to indicate whether the audit event's environment should include the full stack trace
        /// </summary>
        public static bool IncludeStackTrace { get; set; }
        
        /// <summary>
        /// Gets or Sets the value used to indicate whether the audit event should include the activity trace
        /// </summary>
        public static bool IncludeActivityTrace { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether each audit scope should create and start a new Distributed Tracing Activity.
        /// </summary>
        public static bool StartActivityTrace { get; set; }

        /// <summary>
        /// Gets or Sets a value that indicates if the environment information should be excluded from the audit output. Default is false to include the environment object.
        /// </summary>
        public static bool ExcludeEnvironmentInfo { get; set; }
        
        /// <summary>
        /// Gets or Sets the Default audit scope factory.
        /// </summary>
        public static IAuditScopeFactory AuditScopeFactory
        {
            get => _auditScopeFactory;
            set => _auditScopeFactory = value ?? new AuditScopeFactory();
        }

        /// <summary>
        /// Global switch to disable audit logging. Default is false.
        /// </summary>
        public static bool AuditDisabled { get; set; }

        /// <summary>
        /// Global json serializer settings for serializing the audit event on the data providers or by calling the ToJson() method on the AuditEvent.
        /// </summary>
        public static JsonSerializerOptions JsonSettings { get; set; }

        // Custom actions
        internal static ConcurrentDictionary<ActionType, ConcurrentQueue<Func<AuditScope, CancellationToken, Task<bool>>>> AuditScopeActions { get; private set; } = new();

        private static IAuditScopeFactory _auditScopeFactory;

        /// <summary>
        /// Gets or sets the json adapter that controls the JSON serialization mechanism.
        /// </summary>
        public static IJsonAdapter JsonAdapter { get; set; } = new JsonAdapter();

        static Configuration()
        {
            Reset();
        }

        /// <summary>
        /// Resets all the global configurations to its default values. Will also remove all the custom actions. 
        /// </summary>
        public static void Reset()
        {
            AuditDisabled = false;
            DataProvider = new FileDataProvider();
            CreationPolicy = EventCreationPolicy.InsertOnEnd;
            JsonSettings = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = null,
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            };
            SystemClock = new DefaultSystemClock();
            _auditScopeFactory = new AuditScopeFactory();
            IncludeTypeNamespaces = false;
            IncludeStackTrace = false;
            ExcludeEnvironmentInfo = false;
            IncludeActivityTrace = false;
            StartActivityTrace = false;
            JsonAdapter = new JsonAdapter();
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
            AuditScopeActions[when].Enqueue((scope, _) =>
            {
                action.Invoke(scope);
                return Task.FromResult(true);
            });
        }

        /// <summary>
        /// Attaches an action to be performed globally on any AuditScope. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="when">To indicate when the action should be performed.</param>
        /// <param name="action">The action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddCustomAction(ActionType when, Func<AuditScope, bool> action)
        {
            AuditScopeActions[when].Enqueue((scope, _) =>
            { 
                var result = action.Invoke(scope);
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Attaches an asynchronous action to be performed globally on any AuditScope.
        /// </summary>
        /// <param name="when">To indicate when the action should be performed.</param>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddCustomAction(ActionType when, Func<AuditScope, Task> asyncAction)
        {
            AuditScopeActions[when].Enqueue(async (scope, _) =>
            {
                await asyncAction.Invoke(scope);
                return true;
            });
        }

        /// <summary>
        /// Attaches an asynchronous action to be performed globally on any AuditScope. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="when">To indicate when the action should be performed.</param>
        /// <param name="asyncAction">The asynchronous action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddCustomAction(ActionType when, Func<AuditScope, Task<bool>> asyncAction)
        {
            AuditScopeActions[when].Enqueue(async (scope, _) =>
            {
                var result = await asyncAction.Invoke(scope);
                return result;
            });
        }

        /// <summary>
        /// Attaches an asynchronous action to be performed globally on any AuditScope.
        /// </summary>
        /// <param name="when">To indicate when the action should be performed.</param>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddCustomAction(ActionType when, Func<AuditScope, CancellationToken, Task> asyncAction)
        {
            AuditScopeActions[when].Enqueue(async (scope, ct) =>
            {
                await asyncAction.Invoke(scope, ct);
                return true;
            });
        }

        /// <summary>
        /// Attaches an asynchronous action to be performed globally on any AuditScope. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="when">To indicate when the action should be performed.</param>
        /// <param name="asyncAction">The asynchronous action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddCustomAction(ActionType when, Func<AuditScope, CancellationToken, Task<bool>> asyncAction)
        {
            AuditScopeActions[when].Enqueue(asyncAction);
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope before the audit event is saved.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public static void AddOnSavingAction(Action<AuditScope> action)
        {
            AuditScopeActions[ActionType.OnEventSaving].Enqueue((scope, _) =>
            {
                action.Invoke(scope);
                return Task.FromResult(true);
            });
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope before the audit event is saved. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="action">The action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddOnSavingAction(Func<AuditScope, bool> action)
        {
            AuditScopeActions[ActionType.OnEventSaving].Enqueue((scope, _) =>
            {
                var result = action.Invoke(scope);
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope before the audit event is saved.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddOnSavingAction(Func<AuditScope, Task> asyncAction)
        {
            AuditScopeActions[ActionType.OnEventSaving].Enqueue(async (scope, _) =>
            {
                await asyncAction.Invoke(scope);
                return true;
            });
        }

        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope before the audit event is saved. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddOnSavingAction(Func<AuditScope, Task<bool>> asyncAction)
        {
            AuditScopeActions[ActionType.OnEventSaving].Enqueue(async (scope, _) =>
            {
                var result = await asyncAction.Invoke(scope);
                return result;
            });
        }

        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope before the audit event is saved.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddOnSavingAction(Func<AuditScope, CancellationToken, Task> asyncAction)
        {
            AuditScopeActions[ActionType.OnEventSaving].Enqueue(async (scope, ct) =>
            {
                await asyncAction.Invoke(scope, ct);
                return true;
            });
        }

        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope before the audit event is saved. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddOnSavingAction(Func<AuditScope, CancellationToken, Task<bool>> asyncAction)
        {
            AuditScopeActions[ActionType.OnEventSaving].Enqueue(asyncAction);
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope right after it is created and before any saving.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public static void AddOnCreatedAction(Action<AuditScope> action)
        {
            AuditScopeActions[ActionType.OnScopeCreated].Enqueue((scope, _) => 
            {
                action.Invoke(scope);
                return Task.FromResult(true);
            });
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope right after it is created and before any saving. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="action">The action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddOnCreatedAction(Func<AuditScope, bool> action)
        {
            AuditScopeActions[ActionType.OnScopeCreated].Enqueue((scope, _) =>
            {
                var result = action.Invoke(scope);
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope right after it is created and before any saving.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddOnCreatedAction(Func<AuditScope, Task> asyncAction)
        {
            AuditScopeActions[ActionType.OnScopeCreated].Enqueue(async (scope, _) =>
            {
                await asyncAction.Invoke(scope);
                return true;
            });
        }

        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope right after it is created and before any saving. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddOnCreatedAction(Func<AuditScope, Task<bool>> asyncAction)
        {
            AuditScopeActions[ActionType.OnScopeCreated].Enqueue(async (scope, _) =>
            {
                var result = await asyncAction.Invoke(scope);
                return result;
            });
        }

        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope right after it is created and before any saving.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddOnCreatedAction(Func<AuditScope, CancellationToken, Task> asyncAction)
        {
            AuditScopeActions[ActionType.OnScopeCreated].Enqueue(async (scope, ct) =>
            {
                await asyncAction.Invoke(scope, ct);
                return true;
            });
        }


        /// <summary>
        /// Attaches a global asynchronous action to be performed on the audit scope right after it is created and before any saving. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddOnCreatedAction(Func<AuditScope, CancellationToken, Task<bool>> asyncAction)
        {
            AuditScopeActions[ActionType.OnScopeCreated].Enqueue(asyncAction);
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope right after it is disposed.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        public static void AddOnDisposedAction(Action<AuditScope> action)
        {
            AuditScopeActions[ActionType.OnScopeDisposed].Enqueue((scope, _) =>
            {
                action.Invoke(scope);
                return Task.FromResult(true);
            });
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope right after it is disposed. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="action">The action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddOnDisposedAction(Func<AuditScope, bool> action)
        {
            AuditScopeActions[ActionType.OnScopeDisposed].Enqueue((scope, _) =>
            {
                var result = action.Invoke(scope);
                return Task.FromResult(result);
            });
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope right after it is disposed.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddOnDisposedAction(Func<AuditScope, Task> asyncAction)
        {
            AuditScopeActions[ActionType.OnScopeDisposed].Enqueue(async (scope, _) =>
            {
                await asyncAction.Invoke(scope);
                return true;
            });
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope right after it is disposed. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddOnDisposedAction(Func<AuditScope, Task<bool>> asyncAction)
        {
            AuditScopeActions[ActionType.OnScopeDisposed].Enqueue(async (scope, _) =>
            {
                var result = await asyncAction.Invoke(scope);
                return result;
            });
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope right after it is disposed.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform.</param>
        public static void AddOnDisposedAction(Func<AuditScope, CancellationToken, Task> asyncAction)
        {
            AuditScopeActions[ActionType.OnScopeDisposed].Enqueue(async (scope, ct) =>
            {
                await asyncAction.Invoke(scope, ct);
                return true;
            });
        }

        /// <summary>
        /// Attaches a global action to be performed on the audit scope right after it is disposed. The action returns a boolean value indicating if the subsequent actions should be executed.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to perform. Return true to continue with the next actions, false to stop executing the subsequent actions.</param>
        public static void AddOnDisposedAction(Func<AuditScope, CancellationToken, Task<bool>> asyncAction)
        {
            AuditScopeActions[ActionType.OnScopeDisposed].Enqueue(asyncAction);
        }

        /// <summary>
        /// Resets the audit scope handlers. Removes all the attached actions for the Audit Scopes.
        /// </summary>
        public static void ResetCustomActions()
        {
            ResetCustomActions(ActionType.OnScopeCreated);
            ResetCustomActions(ActionType.OnEventSaving);
            ResetCustomActions(ActionType.OnEventSaved);
            ResetCustomActions(ActionType.OnScopeDisposed);
        }

        /// <summary>
        /// Removes all the attached actions for the given action type.
        /// </summary>
        public static void ResetCustomActions(ActionType actionType)
        {
            AuditScopeActions[actionType] = new ConcurrentQueue<Func<AuditScope, CancellationToken, Task<bool>>>();
        }

        /// <summary>
        /// Synchronously invokes the global custom actions.
        /// </summary>
        internal static void InvokeCustomActions(ActionType type, AuditScope auditScope)
        {
            foreach (var action in AuditScopeActions[type])
            {
                var @continue = action.Invoke(auditScope, default).GetAwaiter().GetResult();
                if (!@continue)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Asynchronously invokes the global custom actions.
        /// </summary>
        internal static async Task InvokeCustomActionsAsync(ActionType type, AuditScope auditScope, CancellationToken cancellationToken)
        {
            foreach (var action in AuditScopeActions[type])
            {
                var @continue = await action.Invoke(auditScope, cancellationToken);
                if (!@continue)
                {
                    break;
                }
            }
        }
    }
}
