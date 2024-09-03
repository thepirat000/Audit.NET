﻿using System;

namespace Audit.Core
{
    /// <summary>
    /// The default system clock implementation using DateTime.UtcNow
    /// </summary>
    public class DefaultSystemClock : ISystemClock
    {
        /// <inheritdoc />
        public DateTime GetCurrentDateTime()
        {
            return DateTime.UtcNow;
        }
    }
}
