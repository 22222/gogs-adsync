using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GogsActiveDirectorySync
{
    public static class TimeUtils
    {
        public static TimeSpan? CalculateWaitTime(TimeSpan currentTimeOfDay, TimeSpan? minimumTimeOfDay, TimeSpan? maximumTimeOfDay)
        {
            if (!minimumTimeOfDay.HasValue && !maximumTimeOfDay.HasValue) return default(TimeSpan?);

            if (minimumTimeOfDay > maximumTimeOfDay)
            {
                var temp = minimumTimeOfDay;
                maximumTimeOfDay = minimumTimeOfDay;
                minimumTimeOfDay = temp;
            }

            TimeSpan? waitTimespan;
            if (minimumTimeOfDay.HasValue && currentTimeOfDay < minimumTimeOfDay)
            {
                waitTimespan = minimumTimeOfDay.Value - currentTimeOfDay;
            }
            else if (maximumTimeOfDay.HasValue && currentTimeOfDay > maximumTimeOfDay)
            {
                var minimTimeOfDayOrDefault = minimumTimeOfDay ?? TimeSpan.FromHours(0);
                waitTimespan = TimeSpan.FromHours(24) + minimTimeOfDayOrDefault - currentTimeOfDay;
            }
            else
            {
                waitTimespan = null;
            }
            return waitTimespan;
        }
    }
}
