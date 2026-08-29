using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class UnitOfMeasure : AuditableEntity
{
    [Required(ErrorMessage="وحدة القياس مطلوبة"),MaxLength(100),Display(Name="وحدة القياس")] public string Name{get;set;}="";
    [MaxLength(1000),Display(Name="ملاحظات")] public string? Comments{get;set;}
    public ICollection<Material> Materials{get;set;}=[];
}
