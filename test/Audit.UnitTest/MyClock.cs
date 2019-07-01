using Audit.Core;
using System;

namespace Audit.UnitTest
{
    public class MyClock : ISystemClock
    {
        public DateTimeOffset _start = new DateTime(2020, 1, 1);
        public DateTimeOffset UtcNow
        {
            get
            {
                var dt = _start;
                _start = _start.AddSeconds(10);
                return dt;
            }
        }
    }
}
