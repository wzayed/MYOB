using System.ComponentModel.DataAnnotations;

namespace MYOB.Models;

public class SupplierCredit : AuditableEntity
{
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public Guid SourceSupplierReceiptId { get; set; }
    public SupplierReceipt SourceSupplierReceipt { get; set; } = null!;
    public Guid SourcePaymentGroupId { get; set; }
    public DateOnly Date { get; set; }
    public Guid PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;
    public decimal OriginalAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    [MaxLength(2000)] public string? Comments { get; set; }
}
