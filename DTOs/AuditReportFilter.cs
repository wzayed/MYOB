namespace MYOB.DTOs;
public sealed class AuditReportFilter
{
    public string? UserName { get; set; }
    public string? Screen { get; set; }
    public string? Action { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}
