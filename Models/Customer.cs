using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class Customer : AuditableEntity
{
    [Required(ErrorMessage="اسم العميل مطلوب"),MaxLength(200),Display(Name="اسم العميل")] public string Name{get;set;}="";
    [MaxLength(50),Display(Name="الهاتف")] public string? Phone{get;set;}
    [MaxLength(500),Display(Name="العنوان")] public string? Address{get;set;}
    [Display(Name="الرصيد الافتتاحي")] public decimal OpeningBalance{get;set;}
    [MaxLength(1000),Display(Name="ملاحظات")] public string? Notes{get;set;}
}
