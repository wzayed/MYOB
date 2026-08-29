namespace MYOB.DTOs;
public sealed record AuditReportRow(DateTimeOffset OccurredAt, string UserName, string Screen, string Action, string? EntityName, string? RecordId, string? IpAddress);
