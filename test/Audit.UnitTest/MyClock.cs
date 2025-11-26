using Audit.Core;
using System;

namespace Audit.UnitTest
{
    public class MyClock : ISystemClock
    {
        private DateTime _start = new DateTime(2020, 1, 1);
        private static long _timestampCounter = 0;

        public DateTime GetCurrentDateTime()
        {
            var dt = _start;
            _start = _start.AddSeconds(10);
            return dt;
        }

        public long GetCurrentTimestamp()
        {
            _timestampCounter++;

            return _timestampCounter;
        }
    }
}
