using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class SupplierReceipt : AuditableEntity
{
    [Required] public Guid SupplierId{get;set;} public Supplier Supplier{get;set;}=null!;
    [Required] public Guid MaterialId{get;set;} public Material Material{get;set;}=null!;
    [DataType(DataType.Date)] public DateOnly Date{get;set;}=DateOnly.FromDateTime(DateTime.Today);
    [Required,MaxLength(100),Display(Name="رقم البوليصة / أمر الشحن")] public string PolicyNumber{get;set;}="";
    public decimal Quantity{get;set;} public decimal UnitPrice{get;set;} public decimal TaxPercent{get;set;} public decimal Total{get;set;}
    [MaxLength(2000)] public string? Details{get;set;}
    public ICollection<SupplierPayment> Payments{get;set;}=[];
    public CustomerDelivery? CustomerDelivery{get;set;}
}
