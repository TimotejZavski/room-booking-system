using System;

namespace WebApplication1.Extensions;

public static class DateTimeExtensions
{
    public static DateTime RoundToNearest(this DateTime dt, TimeSpan d)
    {
        var delta = dt.Ticks % d.Ticks;
        bool roundUp = delta > d.Ticks / 2;
        var offset = roundUp ? d.Ticks - delta : -delta;
        return new DateTime(dt.Ticks + offset, dt.Kind);
    }
} 