using Castle.DynamicProxy;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using Audit.Core;
using System;

namespace Audit.DynamicProxy
{
    /// <summary>
    /// Generates audited (proxied) instances of classes.
    /// </summary>
    public class AuditProxy
    {
        private static readonly ProxyGenerator Generator = new ProxyGenerator();
        [ThreadStatic]
        private static IAuditScope _currentScope;

        /// <summary>
        /// Gets the scope for the current running thread. Get this property value from the audited methods to customize the Audit.
        /// This property should be accessed from the thread that is executing the audited operation.
        /// Calling this property from a different thread will return NULL or an unexpected value.
        /// </summary>
        /// <value>The current scope related to the running thread, or NULL.</value>
        public static IAuditScope CurrentScope
        {
            get => _currentScope;
            internal set => _currentScope = value;
        }

        /// <summary>
        /// Creates and returns a proxy for the given instance and base type, using the given settings.
        /// </summary>
        /// <typeparam name="T">The generic type argument T can be:
        ///  - An interface type: To log all the interface member calls.
        ///  - A class (or base class) type: To log all the virtual member calls.</typeparam>
        /// <param name="instance">The instance to proxy.</param>
        /// <param name="settings">The settings to use (or NULL to use the default settings).</param>
        public static T Create<T>(T instance, InterceptionSettings settings = null)
            where T : class
        {
            T auditedInstance;
            // One interceptor per instance
            var interceptor = new AuditInterceptor()
            {
                Settings = settings ?? new InterceptionSettings()
            };
            if (typeof(T).GetTypeInfo().IsInterface)
            {
                auditedInstance = Generator.CreateInterfaceProxyWithTarget<T>(instance, new[] { interceptor }) as T;
            }
            else
            {
                auditedInstance = Generator.CreateClassProxyWithTarget<T>(instance, new[] { interceptor }) as T;
            }
            return auditedInstance;
        }
    }
}
