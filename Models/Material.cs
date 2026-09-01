using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace MYOB.Models;
public class Material : AuditableEntity
{
    [Required(ErrorMessage = "اسم الخامة مطلوب"), MaxLength(200), Display(Name = "اسم الخامة")] public string Name { get; set; } = string.Empty;
    [Required(ErrorMessage = "الوحدة مطلوبة"), Display(Name = "الوحدة")] public Guid UnitOfMeasureId { get; set; }
    [ValidateNever] public UnitOfMeasure UnitOfMeasure { get; set; } = null!;
    [MaxLength(1000), Display(Name = "ملاحظات")] public string? Notes { get; set; }
}
