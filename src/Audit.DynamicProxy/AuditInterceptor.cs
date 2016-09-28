using System;
using System.Collections.Generic;
using Audit.Core;
using Castle.DynamicProxy;
using System.Reflection;
using System.Linq;
using Audit.Core.Extensions;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Castle DynamicProxy Interceptor for Auditing purposes.
    /// </summary>
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

        public void Intercept(IInvocation invocation)
        {
            var intEvent = CreateAuditInterceptEvent(invocation);
            if (intEvent == null)
            {
                // bypass
                invocation.Proceed();
                return;
            }
            var eventType = Settings.EventType?.Replace("{class}", intEvent.ClassName).Replace("{method}", intEvent.MethodName);
            var scope = AuditScope.Create(eventType, null, EventCreationPolicy.Manual, Settings.AuditDataProvider);
            scope.SetCustomField("InterceptEvent", intEvent);
            AuditProxy.CurrentScope = scope;
            try
            {
                invocation.Proceed();
            }
            catch (Exception ex)
            {
                intEvent.Exception = ex.GetExceptionInfo();
                scope.Save();
                AuditProxy.CurrentScope = null;
                throw;
            }
            SuccessAuditInterceptEvent(invocation, intEvent);
            scope.Save();
            scope.Dispose();
            AuditProxy.CurrentScope = null;
        }
        
        private void SuccessAuditInterceptEvent(IInvocation invocation, AuditInterceptEvent intEvent)
        {
            var method = invocation.MethodInvocationTarget;
            intEvent.Success = true;
            intEvent.Result = new AuditInterceptArgument(method.ReturnType, invocation.ReturnValue);
            // update the output param values
            int i = 0;
            foreach (var p in method.GetParameters())
            {
                if (p.ParameterType.IsByRef)
                {
                    intEvent.Arguments[i].OutputValue = invocation.Arguments[i];
                }
                i++;
            }
        }

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
                MethodSignature = invocation.MethodInvocationTarget.ToString(),
                PropertyName = isProp ? method.Name.Substring(method.Name.IndexOf('_') + 1) : null,
                EventName = isEvent ? method.Name.Substring(method.Name.IndexOf('_') + 1) : null,
                Arguments = GetInputParams(invocation)
            };
            return intEvent;
        }

        private List<AuditInterceptArgument> GetInputParams(IInvocation invocation)
        {
            var result = new List<AuditInterceptArgument>();
            var method = invocation.MethodInvocationTarget;
            int i = 0;
            foreach (var p in method.GetParameters())
            {
                result.Add(new AuditInterceptArgument(p.Name, p.ParameterType, invocation.Arguments[i]));
                i++;
            }
            return result.Count == 0 ? null : result;
        }
    }
}