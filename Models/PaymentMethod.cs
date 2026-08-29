using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class PaymentMethod : AuditableEntity
{
    [Required(ErrorMessage="طريقة الدفع مطلوبة"),MaxLength(150),Display(Name="طريقة الدفع")] public string Name{get;set;}="";
    [MaxLength(1000),Display(Name="ملاحظات")] public string? Notes{get;set;}
}
