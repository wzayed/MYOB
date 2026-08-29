using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public abstract class AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(256)] public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    [MaxLength(256)] public string? UpdatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    [MaxLength(256)] public string? DeletedBy { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
}
