#if EF_CORE_5_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Collections.Generic;
using System.Data;
using System.Text.Json.Serialization;

namespace Audit.EntityFramework
{
    /// <summary>
    /// Event information for command interception
    /// </summary>
    public class CommandEvent : InterceptorEventBase
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
        /// The command text
        /// </summary>
        public string CommandText { get; set; }
        /// <summary>
        /// The parameter values
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; }
        /// <summary>
        /// The result of the command execution
        /// </summary>
        public object Result { get; set; }

        [JsonIgnore]
        internal DbContext DbContext { get; set; }

        /// <summary>
        /// Returns the DbContext associated to this event
        /// </summary>
        public DbContext GetDbContext()
        {
            return DbContext;
        }
    }
}
#endif