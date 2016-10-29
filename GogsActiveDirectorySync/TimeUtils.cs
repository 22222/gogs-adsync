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

            TimeSpan? waitTimespan = null;
            bool isInverted = minimumTimeOfDay.HasValue && maximumTimeOfDay.HasValue && minimumTimeOfDay.Value > maximumTimeOfDay.Value;
            if (isInverted)
            {
                if (currentTimeOfDay < minimumTimeOfDay.Value && currentTimeOfDay > maximumTimeOfDay.Value)
                {
                    waitTimespan = minimumTimeOfDay.Value - currentTimeOfDay;
                }
            }
            else
            {
                if (minimumTimeOfDay.HasValue && currentTimeOfDay < minimumTimeOfDay.Value)
                {
                    waitTimespan = minimumTimeOfDay.Value - currentTimeOfDay;
                }
                else if (maximumTimeOfDay.HasValue && currentTimeOfDay > maximumTimeOfDay.Value)
                {
                    var minimTimeOfDayOrDefault = minimumTimeOfDay ?? TimeSpan.FromHours(0);
                    waitTimespan = TimeSpan.FromHours(24) + minimTimeOfDayOrDefault - currentTimeOfDay;
                }
            }
            return waitTimespan;
        }
    }
}
