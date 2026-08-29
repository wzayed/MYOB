using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.DTOs;
namespace MYOB.Services;
public interface IAuditReportService { Task<List<AuditReportRow>> GetAsync(AuditReportFilter filter); }
public sealed class AuditReportService(ApplicationDbContext db) : IAuditReportService
{
    public async Task<List<AuditReportRow>> GetAsync(AuditReportFilter filter)
    {
        var query = db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.UserName)) query = query.Where(x => x.UserName.Contains(filter.UserName));
        if (!string.IsNullOrWhiteSpace(filter.Screen)) query = query.Where(x => x.Screen.Contains(filter.Screen));
        if (!string.IsNullOrWhiteSpace(filter.Action)) query = query.Where(x => x.Action == filter.Action);
        if (filter.FromDate is { } from) query = query.Where(x => x.OccurredAt >= AtLocalMidnight(from));
        if (filter.ToDate is { } to) query = query.Where(x => x.OccurredAt < AtLocalMidnight(to.AddDays(1)));
        return await query.OrderByDescending(x => x.OccurredAt)
            .Select(x => new AuditReportRow(x.OccurredAt, x.UserName, x.Screen, x.Action, x.EntityName, x.RecordId, x.IpAddress)).ToListAsync();
    }
    private static DateTimeOffset AtLocalMidnight(DateOnly date)
    {
        var local = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
    }
}
