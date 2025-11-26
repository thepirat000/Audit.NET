using System;

namespace Audit.Core
{
    /// <summary>
    /// Abstracts the system clock.
    /// </summary>
    public interface ISystemClock
    {
        /// <summary>
        /// Retrieves the current system time to be stored in the audit event.
        /// </summary>
        /// <returns></returns>
        DateTime GetCurrentDateTime();

        /// <summary>
        /// Retrieves the current timestamp to be stored in the audit event.
        /// </summary>
        /// <returns></returns>
        long GetCurrentTimestamp();
    }
}
