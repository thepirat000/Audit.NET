using System;
using System.Linq;

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
            if (exception is AggregateException)
            {
                var aggEx = exception as AggregateException;
                return string.Join(" | ", aggEx.InnerExceptions?.Select(ex => GetExceptionInfo(ex)));
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
