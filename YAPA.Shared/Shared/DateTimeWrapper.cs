using System;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Shared
{
    public class DateTimeWrapper : IDate
    {
        public DateTime DateTimeUtc()
        {
            return DateTime.UtcNow;
        }
    }
}
