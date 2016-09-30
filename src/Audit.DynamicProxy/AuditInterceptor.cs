using System;
using System.Collections.Generic;
using Audit.Core;
using Castle.DynamicProxy;
using System.Reflection;
using System.Linq;
using Audit.Core.Extensions;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Castle DynamicProxy Interceptor for Auditing purposes.
    /// </summary>
    /// <remarks>
    /// Ideas stolen from:
    /// https://blog.cincura.net/233489-injecting-logging-into-asynchronous-methods
    /// http://stackoverflow.com/questions/28099669/intercept-async-method-that-returns-generic-task-via-dynamicproxy
    /// </remarks>
#if NET45
    [Serializable]
#endif
    public class AuditInterceptor : IInterceptor
    {
        /// <summary>
        /// Gets or sets the current settings fot this interceptor.
        /// </summary>
        /// <value>The settings.</value>
        public InterceptionSettings Settings { get; set; }

        #region Private Methods
        /// <summary>
        /// Intercept an asynchronous operation that returns a Task.
        /// </summary>
        private static async Task InterceptAsync(Task task, IInvocation invocation, AuditInterceptEvent intEvent, AuditScope scope)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch
            {
                EndAsyncAuditInterceptEvent(task, invocation, intEvent, scope, null);
                throw;
            }
            EndAsyncAuditInterceptEvent(task, invocation, intEvent, scope, "Void");
        }

        /// <summary>
        /// Intercept an asynchronous operation that returns a Task Of[T].
        /// </summary>
        private static async Task<T> InterceptAsync<T>(Task<T> task, IInvocation invocation, AuditInterceptEvent intEvent, AuditScope scope)
        {
            T result;
            try
            {
                result = await task.ConfigureAwait(false);
            }
            catch
            {
                EndAsyncAuditInterceptEvent(task, invocation, intEvent, scope, null);
                throw;
            }
            EndAsyncAuditInterceptEvent(task, invocation, intEvent, scope, result);
            return result;
        }

        /// <summary>
        /// Ends the event for asynchronous interceptions.
        /// </summary>
        private static void EndAsyncAuditInterceptEvent(Task task, IInvocation invocation, AuditInterceptEvent intEvent, AuditScope scope, object result)
        {
            intEvent.AsyncStatus = task.Status.ToString();
            if (task.Status == TaskStatus.Faulted)
            {
                intEvent.Exception = task.Exception?.GetExceptionInfo();
            }
            else if (task.Status == TaskStatus.RanToCompletion)
            {
                SuccessAuditInterceptEvent(invocation, intEvent, result);
            }
            scope.Save();
        }

        /// <summary>
        /// Ends the event successfully.
        /// </summary>
        /// <param name="invocation">The invocation.</param>
        /// <param name="intEvent">The int event.</param>
        /// <param name="returnValue">The return value.</param>
        private static void SuccessAuditInterceptEvent(IInvocation invocation, AuditInterceptEvent intEvent, object returnValue)
        {
            var method = invocation.MethodInvocationTarget;
            intEvent.Success = true;
            if (IncludeReturnValue(method))
            {
                intEvent.Result = new AuditInterceptArgument(method.ReturnType, returnValue);
            }
            // update the output param values
            if (intEvent.Arguments != null)
            {
                var methodParams = method.GetParameters();
                for (int i = 0; i < intEvent.Arguments.Count; i++)
                {
                    var arg = intEvent.Arguments[i];
                    if (methodParams[arg.Index.Value].ParameterType.IsByRef)
                    {
                        arg.OutputValue = invocation.Arguments[arg.Index.Value];
                    }
                }
            }
        }

        private static bool IncludeReturnValue(MethodInfo method)
        {
            var ignoreAttrs = method.ReturnTypeCustomAttributes.GetCustomAttributes(typeof(AuditIgnoreAttribute), true);
            return ignoreAttrs == null || ignoreAttrs.Length == 0;
        }

        /// <summary>
        /// Creates the audit intercept event. Returns NULL if the event should be bypassed
        /// </summary>
        private AuditInterceptEvent CreateAuditInterceptEvent(IInvocation invocation)
        {
            var method = invocation.MethodInvocationTarget;
            if (method == null)
            {
                // operation is not implemented
                return null;
            }
            bool ignore = method.GetCustomAttributes(typeof(AuditIgnoreAttribute), true).FirstOrDefault() != null;
            if (ignore)
            {
                // operation is explicitly ignored
                return null;
            }
            bool isProp = method.IsSpecialName && (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"));
            if (isProp && Settings.IgnoreProperties)
            {
                // operation is a property getter/setter and should be ignored
                return null;
            }
            bool isEvent = method.IsSpecialName && (method.Name.StartsWith("add_") || method.Name.StartsWith("remove_"));
            if (isEvent && Settings.IgnoreEvents)
            {
                // operation is an event attach/detach and should be ignored
                return null;
            }
            if (Settings.MethodFilter != null)
            {
                if (!Settings.MethodFilter.Invoke(method))
                {
                    // operation was filtered out
                    return null;
                }
            }
            var intEvent = new AuditInterceptEvent()
            {
                ClassName = invocation.TargetType.Name,
                InstanceQualifiedName = invocation.TargetType.AssemblyQualifiedName,
                MethodName = method.Name,
                MethodSignature = method.ToString(),
                PropertyName = isProp ? method.Name.Substring(method.Name.IndexOf('_') + 1) : null,
                EventName = isEvent ? method.Name.Substring(method.Name.IndexOf('_') + 1) : null,
                Arguments = GetInputParams(invocation)
            };
            return intEvent;
        }

        /// <summary>
        /// Gets the input parameters from the invocation.
        /// </summary>
        /// <param name="invocation">The invocation.</param>
        private static List<AuditInterceptArgument> GetInputParams(IInvocation invocation)
        {
            var result = new List<AuditInterceptArgument>();
            var method = invocation.MethodInvocationTarget;
            int i = 0;
            foreach (var p in method.GetParameters())
            {
                if (p.GetCustomAttribute(typeof(AuditIgnoreAttribute), true) == null)
                {
                    result.Add(new AuditInterceptArgument(p.Name, p.ParameterType, invocation.Arguments[i], i));
                }
                i++;
            }
            return result.Count == 0 ? null : result;
        }
        #endregion

        #region IInterceptor implementation
        /// <summary>
        /// Intercepts the specified invocation.
        /// </summary>
        public void Intercept(IInvocation invocation)
        {
            var intEvent = CreateAuditInterceptEvent(invocation);
            if (intEvent == null)
            {
                // bypass
                invocation.Proceed();
                return;
            }
            var method = invocation.MethodInvocationTarget;
            var eventType = Settings.EventType?.Replace("{class}", intEvent.ClassName).Replace("{method}", intEvent.MethodName);
            var scope = AuditScope.Create(eventType, null, EventCreationPolicy.Manual, Settings.AuditDataProvider);
            var isAsync = method.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null;
            intEvent.IsAsync = isAsync;
            scope.SetCustomField("InterceptEvent", intEvent);
            AuditProxy.CurrentScope = scope;
            // Call the intercepted method (sync part)
            try
            {
                invocation.Proceed();
            }
            catch (Exception ex)
            {
                intEvent.Exception = ex.GetExceptionInfo();
                scope.Save();
                throw;
            }
            // Handle async calls
            var returnType = method.ReturnType;
            if (isAsync)
            {
                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    invocation.ReturnValue = InterceptAsync((dynamic)invocation.ReturnValue, invocation, intEvent, scope);
                    return;
                }
            }
            // Is a Sync method (or an Async method that does not returns a Task or Task<>).
            // Avoid Task and Task<T> serialization (i.e. when a sync method returns a Task)
            object returnValue = typeof(Task).IsAssignableFrom(returnType) ? null : invocation.ReturnValue;
            SuccessAuditInterceptEvent(invocation, intEvent, returnValue);
            scope.Save();
            if (!isAsync)
            {
                AuditProxy.CurrentScope = null;
            }
        }
        #endregion
    }
}