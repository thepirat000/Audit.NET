using System;

namespace Audit.Core
{
    /// <summary>
    /// Abstracts the system clock.
    /// </summary>
    public interface ISystemClock
    {
        /// <summary>
        /// Retrieves the current system time in UTC.
        /// </summary>
        DateTimeOffset UtcNow
        {
            get;
        }
    }
}
