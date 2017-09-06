using System;
using YAPA.Shared.Contracts;

namespace YAPA.Shared.Common
{
    public class DateTimeWrapper : IDate
    {
        public DateTime DateTimeUtc()
        {
            return DateTime.UtcNow;
        }
    }
}
