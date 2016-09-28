using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Audit.Core.Extensions
{
    /// <summary>
    /// Exception extension methods
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Gets a string including the type and messages for the given exception and its inner exceptions.
        /// </summary>
        /// <param name="exception">The exception</param>
        public static string GetExceptionInfo(this Exception exception)
        {
            if (exception == null)
            {
                return null;
            }
            string exceptionInfo = $"({exception.GetType().Name}) {exception.Message}";
            Exception inner = exception;
            while ((inner = inner.InnerException) != null)
            {
                exceptionInfo += " -> " + inner.Message;
            }
            return exceptionInfo;
        }
    }
}
