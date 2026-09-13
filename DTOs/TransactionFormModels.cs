using System.ComponentModel.DataAnnotations;
using MYOB.Support;
namespace MYOB.DTOs;
public sealed class SupplierReceiptForm
{
 public Guid Id{get;set;}[Required(ErrorMessage="اختر المورد")]public Guid SupplierId{get;set;}[Required(ErrorMessage="اختر الخامة")]public Guid MaterialId{get;set;}[Required][NotFutureDate][DisplayFormat(DataFormatString="{0:yyyy/MM/dd}",ApplyFormatInEditMode=true)]public DateOnly Date{get;set;}=BusinessDate.Today;[Required(ErrorMessage="رقم البوليصة مطلوب")]public string PolicyNumber{get;set;}="";[Range(typeof(decimal),"0.0001","999999999")]public decimal Quantity{get;set;}[Range(typeof(decimal),"0","999999999")]public decimal UnitPrice{get;set;}[Range(typeof(decimal),"0","100")]public decimal TaxPercent{get;set;}public string? Details{get;set;}
}
public sealed class SupplierPaymentForm
{
 public Guid Id{get;set;}[Required]public Guid SupplierId{get;set;}[Required]public Guid SupplierReceiptId{get;set;}[Required]public DateOnly Date{get;set;}=DateOnly.FromDateTime(DateTime.Today);[Range(typeof(decimal),"0.01","999999999")]public decimal Amount{get;set;}[Required]public Guid PaymentMethodId{get;set;}[MaxLength(2000)]public string? Comments{get;set;}[MaxLength(2000)]public string? PublicComments{get;set;}
}
public sealed class SupplierPaymentBatchForm
{
 public Guid PaymentGroupId{get;set;}public Guid? SupplierCreditId{get;set;}[Required]public Guid SupplierId{get;set;}[Required][NotFutureDate][DisplayFormat(DataFormatString="{0:yyyy/MM/dd}",ApplyFormatInEditMode=true)]public DateOnly Date{get;set;}=BusinessDate.Today;[Range(typeof(decimal),"0.01","999999999",ErrorMessage="أدخل إجمالي المبلغ المدفوع")]public decimal TotalAmount{get;set;}[Required]public Guid PaymentMethodId{get;set;}[MaxLength(2000)]public string? Comments{get;set;}[MaxLength(2000)]public string? PublicComments{get;set;}public List<SupplierPaymentAllocationForm> Allocations{get;set;}=[];
}
public sealed class SupplierPaymentAllocationForm
{
 [Required]public Guid SupplierReceiptId{get;set;}[Range(typeof(decimal),"0","999999999")]public decimal Amount{get;set;}
}
public sealed class CustomerDeliveryForm
{
 public Guid Id{get;set;}[Required]public Guid CustomerId{get;set;}[Required]public Guid SupplierReceiptId{get;set;}[Required][NotFutureDate][DisplayFormat(DataFormatString="{0:yyyy/MM/dd}",ApplyFormatInEditMode=true)]public DateOnly Date{get;set;}=BusinessDate.Today;[Range(typeof(decimal),"0","999999999")]public decimal UnitPrice{get;set;}[Range(typeof(decimal),"0","100")]public decimal TaxPercent{get;set;}public string? Details{get;set;}
}
public sealed class CustomerPaymentForm
{
 public Guid Id{get;set;}[Required]public Guid CustomerId{get;set;}[Required]public Guid CustomerDeliveryId{get;set;}[Required]public DateOnly Date{get;set;}=DateOnly.FromDateTime(DateTime.Today);[Range(typeof(decimal),"0.01","999999999")]public decimal Amount{get;set;}[Required]public Guid PaymentMethodId{get;set;}[MaxLength(2000)]public string? Comments{get;set;}[MaxLength(2000)]public string? PublicComments{get;set;}
}
public sealed class CustomerPaymentBatchForm
{
 public Guid PaymentGroupId{get;set;}[Required]public Guid CustomerId{get;set;}[Required][NotFutureDate][DisplayFormat(DataFormatString="{0:yyyy/MM/dd}",ApplyFormatInEditMode=true)]public DateOnly Date{get;set;}=BusinessDate.Today;[Range(typeof(decimal),"0.01","999999999",ErrorMessage="أدخل إجمالي المبلغ المستلم")]public decimal TotalAmount{get;set;}[Required]public Guid PaymentMethodId{get;set;}[MaxLength(2000)]public string? Comments{get;set;}[MaxLength(2000)]public string? PublicComments{get;set;}public List<CustomerPaymentAllocationForm> Allocations{get;set;}=[];
}
public sealed class CustomerPaymentAllocationForm
{
 [Required]public Guid CustomerDeliveryId{get;set;}[Range(typeof(decimal),"0","999999999")]public decimal Amount{get;set;}
}
