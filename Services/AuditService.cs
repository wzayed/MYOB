using MYOB.Data;
using MYOB.Models;
namespace MYOB.Services;
public interface IAuditService { Task WriteAsync(HttpContext context, string action, string screen, string? userName = null, string? entity = null, string? recordId = null, string? oldValues = null, string? newValues = null); }
public sealed class AuditService(ApplicationDbContext db) : IAuditService
{
    public async Task WriteAsync(HttpContext context, string action, string screen, string? userName = null, string? entity = null, string? recordId = null, string? oldValues = null, string? newValues = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
            UserName = userName ?? context.User.Identity?.Name ?? "النظام", Action = action, Screen = screen,
            EntityName = entity, RecordId = recordId, OldValues = oldValues, NewValues = newValues,
            IpAddress = context.Connection.RemoteIpAddress?.ToString(), OccurredAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
