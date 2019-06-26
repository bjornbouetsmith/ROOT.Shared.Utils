using System;

namespace ROOT.Shared.Utils.Date
{
    public class DateUtils
    {
        public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        public static DateTime ToDateTime(long timeSinceEpoch)
        {
            return Epoch.AddSeconds(timeSinceEpoch);
        }

    }
}
