#if EF_CORE_3 || EF_CORE_5 || EF_CORE_6
namespace Audit.EntityFramework
{
    /// <summary>
    /// Base class for the interceptor events
    /// </summary>
    public abstract class InterceptorEventBase
    {
        /// <summary>
        /// The database name
        /// </summary>
        public virtual string Database { get; set; }

        /// <summary>
        /// A unique identifier for the client database connection.
        /// This identifier is primarily intended as a correlation ID for logging and debugging such
        /// that it is easy to identify that multiple events are using the same or different database connection.
        /// </summary>
        public virtual string ConnectionId { get; set; }

        /// <summary>
        /// A correlation ID that identifies the DbConnection instance being used.
        /// This identifier is primarily intended as a correlation ID for logging and debugging such
        /// that it is easy to identify that multiple events are using the same or different database connection.
        /// </summary>
        public virtual string DbConnectionId { get; set; }

        /// <summary>
        /// A unique identifier for the context instance and pool lease, if any.
        /// This identifier is primarily intended as a correlation ID for logging and debugging such
        /// that it is easy to identify that multiple events are using the same or different context instances.
        /// </summary>
        public virtual string ContextId { get; set; }

        /// <summary>
        /// The Transaction ID within which this event was executed
        /// </summary>
        public virtual string TransactionId { get; set; }

        /// <summary>
        /// Boolean value indicating whether the call is asynchronous
        /// </summary>
        public virtual bool IsAsync { get; set; }

        /// <summary>
        /// Boolean to indicate success. Null until command is executed
        /// </summary>
        public virtual bool? Success { get; set; }

        /// <summary>
        /// The exception error message when Success is false
        /// </summary>
        public virtual string ErrorMessage { get; set; }
    }
}
#endif