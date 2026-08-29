using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class CustomerPayment : AuditableEntity
{
    public Guid CustomerId{get;set;} public Customer Customer{get;set;}=null!;
    public Guid CustomerDeliveryId{get;set;} public CustomerDelivery CustomerDelivery{get;set;}=null!;
    [DataType(DataType.Date)] public DateOnly Date{get;set;}=DateOnly.FromDateTime(DateTime.Today);
    public decimal Amount{get;set;}
    public Guid PaymentMethodId{get;set;} public PaymentMethod PaymentMethod{get;set;}=null!;
    [MaxLength(2000)] public string? Comments{get;set;}
}
