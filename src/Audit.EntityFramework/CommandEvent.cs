#if EF_CORE_3 || EF_CORE_5 || EF_CORE_6
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
using System.Data;

namespace Audit.EntityFramework
{
    public class CommandEvent
    {
        /// <summary>
        /// The command method (NonQuery, Scalar, Reader)
        /// </summary>
        public DbCommandMethod Method { get; set; }
        /// <summary>
        /// The command type (Text, StoredProcedure)
        /// </summary>
        public CommandType CommandType { get; set; }
        /// <summary>
        /// The database name
        /// </summary>
        public string Database { get; set; }
        /// <summary>
        /// A unique identifier for the database connection.
        /// This identifier is primarily intended as a correlation ID for logging and debugging such
        /// that it is easy to identify that multiple events are using the same or different database connection.
        /// </summary>
        public string ConnectionId { get; set; }
        /// <summary>
        /// A unique identifier for the context instance and pool lease, if any.
        /// This identifier is primarily intended as a correlation ID for logging and debugging such
        /// that it is easy to identify that multiple events are using the same or different context instances.
        /// </summary>
        public string ContextId { get; set; }
        /// <summary>
        /// The command text
        /// </summary>
        public string CommandText { get; set; }
        /// <summary>
        /// The parameter values
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }
        /// <summary>
        /// Boolean value indicating whether the call is asynchronous
        /// </summary>
        public bool IsAsync { get; set; }
        /// <summary>
        /// Boolean to indicate success. Null until command is executed
        /// </summary>
        public bool? Success { get; set; }
        /// <summary>
        /// The exception error message when Success is false
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// The result of the command execution
        /// </summary>
        public object Result { get; set; }
    }
}
#endif