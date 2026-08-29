using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class AuditLog
{
    public long Id { get; set; }
    [MaxLength(450)] public string? UserId { get; set; }
    [MaxLength(256)] public string UserName { get; set; } = string.Empty;
    [MaxLength(100)] public string Action { get; set; } = string.Empty;
    [MaxLength(300)] public string Screen { get; set; } = string.Empty;
    [MaxLength(200)] public string? EntityName { get; set; }
    [MaxLength(100)] public string? RecordId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    [MaxLength(64)] public string? IpAddress { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}
