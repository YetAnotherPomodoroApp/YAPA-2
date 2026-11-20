using System;

namespace YAPA.Shared.Common
{
    /// <summary>
    /// Completed pomodoros are stored with UTC timestamps. These helpers translate
    /// between those timestamps and the calendar days of a time zone.
    /// </summary>
    public static class LocalDay
    {
        /// <summary>
        /// Wall-clock time in <paramref name="zone"/> of a stored timestamp.
        /// Timestamps read back from the database have no kind and are UTC.
        /// </summary>
        public static DateTime ToLocal(DateTime stored, TimeZoneInfo zone)
        {
            var utc = stored.Kind == DateTimeKind.Local
                ? stored.ToUniversalTime()
                : DateTime.SpecifyKind(stored, DateTimeKind.Utc);

            return DateTime.SpecifyKind(utc + zone.GetUtcOffset(utc), DateTimeKind.Unspecified);
        }

        /// <summary>
        /// UTC moment at which the calendar day of <paramref name="day"/> starts in <paramref name="zone"/>.
        /// </summary>
        public static DateTime StartUtc(DateTime day, TimeZoneInfo zone)
        {
            var midnight = new DateTime(day.Year, day.Month, day.Day, 0, 0, 0, DateTimeKind.Unspecified);

            return DateTime.SpecifyKind(midnight - zone.GetUtcOffset(midnight), DateTimeKind.Utc);
        }
    }
}
