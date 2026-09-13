using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class SupplierPayment : AuditableEntity
{
    public Guid SupplierId{get;set;} public Supplier Supplier{get;set;}=null!;
    public Guid? PaymentGroupId{get;set;}
    public Guid? SupplierCreditId{get;set;} public SupplierCredit? SupplierCredit{get;set;}
    public decimal CreditAmount{get;set;}
    public Guid SupplierReceiptId{get;set;} public SupplierReceipt SupplierReceipt{get;set;}=null!;
    [DataType(DataType.Date)] public DateOnly Date{get;set;}=DateOnly.FromDateTime(DateTime.Today);
    public decimal Amount{get;set;}
    public Guid PaymentMethodId{get;set;} public PaymentMethod PaymentMethod{get;set;}=null!;
    [MaxLength(2000)] public string? Comments{get;set;}
    [MaxLength(2000)] public string? PublicComments { get; set; }
}
