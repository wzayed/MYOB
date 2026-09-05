namespace MYOB.DTOs;
using System.ComponentModel.DataAnnotations;
using MYOB.Support;
public sealed class AuditReportFilter
{
    public string? UserName { get; set; }
    public string? Screen { get; set; }
    public string? Action { get; set; }
    [NotFutureDate, DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)] public DateOnly? FromDate { get; set; }
    [NotFutureDate, DisplayFormat(DataFormatString = "{0:yyyy/MM/dd}", ApplyFormatInEditMode = true)] public DateOnly? ToDate { get; set; }
}
