using System;

namespace YAPA.Shared.Common
{
    public static class DateTimeExtensions
    {
        public static DateTime TryToLocalTime(this DateTime date)
        {
            try
            {
                if(date.Kind == DateTimeKind.Local)
                {
                    return date;
                }
                var specified = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                var local = specified.ToLocalTime();
                return local;
            }
            catch
            {
                return date;
            }
        }
    }
}
