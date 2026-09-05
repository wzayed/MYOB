namespace MYOB.Support;

public static class BusinessDate
{
    private static readonly TimeZoneInfo CairoTimeZone = FindCairoTimeZone();

    public static DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CairoTimeZone).DateTime);

    private static TimeZoneInfo FindCairoTimeZone()
    {
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
