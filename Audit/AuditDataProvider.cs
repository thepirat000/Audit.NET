using System;
using Newtonsoft.Json.Linq;

namespace Audit.Core
{
    /// <summary>
    /// Base class for the persistence classes.
    /// </summary>
    public abstract class AuditDataProvider : IAuditDataProvider
    {
        /// <summary>
        /// Saves the specified audit event.
        /// Triggered when the scope is saved.
        /// Override this method to save the specified audit event.
        /// </summary>
        /// <param name="auditEvent">The audit event.</param>
        public abstract void WriteEvent(AuditEvent auditEvent);

        /// <summary>
        /// Override this method to provide a different serialization method for the values that need to be serialized before saving.
        /// (old target value & custom fields)
        /// </summary>
        public virtual object Serialize<T>(T value)
        {
            if (value == null)
            {
                return null;
            }
            return JToken.FromObject(value);
        }

        /// <summary>
        /// Tests the connection.
        /// </summary>
        public virtual bool TestConnection()
        {
            return true;
        }

        /// <summary>
        /// Initialization method, called when the scope is created
        /// </summary>
        /// <param name="auditEvent">The audit event being created.</param>
        public virtual void Initialize(AuditEvent auditEvent)
        {
            return;
        }
    }
}
