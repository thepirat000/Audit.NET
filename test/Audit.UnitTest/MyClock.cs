using Audit.Core;
using System;

namespace Audit.UnitTest
{
    public class MyClock : ISystemClock
    {
        public DateTime _start = new DateTime(2020, 1, 1);
        public DateTime UtcNow
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
