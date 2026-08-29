using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class CustomerDelivery : AuditableEntity
{
    public Guid CustomerId{get;set;} public Customer Customer{get;set;}=null!;
    public Guid SupplierReceiptId{get;set;} public SupplierReceipt SupplierReceipt{get;set;}=null!;
    [DataType(DataType.Date)] public DateOnly Date{get;set;}=DateOnly.FromDateTime(DateTime.Today);
    public decimal UnitPrice{get;set;} public decimal TaxPercent{get;set;} public decimal Total{get;set;}
    [MaxLength(2000)] public string? Details{get;set;}
    public ICollection<CustomerPayment> Payments{get;set;}=[];
}
