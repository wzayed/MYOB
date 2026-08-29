using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class Supplier : AuditableEntity
{
    [Required(ErrorMessage = "اسم المورد مطلوب"), MaxLength(200), Display(Name = "اسم المورد")] public string Name { get; set; } = string.Empty;
    [MaxLength(50), Display(Name = "الهاتف")] public string? Phone { get; set; }
    [MaxLength(500), Display(Name = "العنوان")] public string? Address { get; set; }
    [Display(Name = "الرصيد الافتتاحي")] public decimal OpeningBalance { get; set; }
    [MaxLength(1000), Display(Name = "ملاحظات")] public string? Notes { get; set; }
}
