using System;

namespace Audit.Core
{
    /// <summary>
    /// The default system clock implementation using DateTime.UtcNow
    /// </summary>
    public class DefaultSystemClock : ISystemClock
    {
        public virtual DateTime UtcNow => DateTime.UtcNow;
    }
}
