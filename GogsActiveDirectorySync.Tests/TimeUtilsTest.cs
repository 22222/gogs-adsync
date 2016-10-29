using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GogsActiveDirectorySync
{
    public class TimeUtilsTest
    {
        [TestCase(0, 5, 10, 5)]
        [TestCase(0, 5, null, 5)]
        [TestCase(17, 0, 5, 7)]
        [TestCase(17, 4, 5, 11)]
        [TestCase(17, 5, 5, 12)]
        [TestCase(17, 20, 5, 3)]
        [TestCase(6, 20, 5, 14)]
        [TestCase(17, 15, 18, null)]
        [TestCase(17, 17, 17, null)]
        [TestCase(17, 0, null, null)]
        [TestCase(17, null, 20, null)]
        [TestCase(17, null, null, null)]
        [TestCase(7, 18, 8, null)]
        [TestCase(20, 18, 8, null)]
        [TestCase(17, 18, 8, 1)]
        [TestCase(9, 18, 8, 9)]
        public void CalculateWaitTime(int currentHour, int? minimumHour, int? maximumHour, int? expectedHour)
        {
            var currentTime = TimeSpan.FromHours(currentHour);
            var minimumTime = minimumHour.HasValue ? TimeSpan.FromHours(minimumHour.Value) : default(TimeSpan?);
            var maximumTime = maximumHour.HasValue ? TimeSpan.FromHours(maximumHour.Value) : default(TimeSpan?);
            var expectedTime = expectedHour.HasValue ? TimeSpan.FromHours(expectedHour.Value) : default(TimeSpan?);

            var waitTime = TimeUtils.CalculateWaitTime(currentTime, minimumTime, maximumTime);
            Assert.AreEqual(expectedTime, waitTime);
        }
    }
}
