using System;

namespace WingsEmu.Core.Extensions;

public static class DateTimeExtension
{
    public static int GetTotalMillisecondUntilNow(this DateTime source) => (int)(source - DateTime.UtcNow).TotalMilliseconds;
}