using System.Collections.Generic;
using Audit.NLog;

namespace Audit.Core
{
    public static class NLogDesearializerExtensions
    {
        public static Audit.NLog.NLogObject NLogDeserialize(this string logEntry)
        {
            return new NLogObject(logEntry);
        }

        public static Audit.NLog.NLogObject[] NLogDeserialize(this IList<string> logEntries)
        {
            var results = new Audit.NLog.NLogObject[logEntries.Count];
            for (var i = 0; i < logEntries.Count; i++)
            {
                results[i] = new NLogObject(logEntries[i]);
            }

            return results;
        }
    }
}
