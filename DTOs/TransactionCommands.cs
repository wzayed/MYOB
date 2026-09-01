namespace MYOB.DTOs;
public sealed record SupplierReceiptCommand(Guid SupplierId,Guid MaterialId,DateOnly Date,string PolicyNumber,decimal Quantity,decimal UnitPrice,decimal TaxPercent,string? Details);
public sealed record SupplierPaymentCommand(Guid SupplierId,Guid SupplierReceiptId,DateOnly Date,decimal Amount,Guid PaymentMethodId,string? Comments);
public sealed record CustomerDeliveryCommand(Guid CustomerId,Guid SupplierReceiptId,DateOnly Date,decimal UnitPrice,decimal TaxPercent,string? Details);
public sealed record CustomerPaymentCommand(Guid CustomerId,Guid CustomerDeliveryId,DateOnly Date,decimal Amount,Guid PaymentMethodId,string? Comments);
public sealed record PolicyOption(Guid Id,string PolicyNumber,string MaterialName,string UnitName,decimal Quantity,decimal RemainingAmount,decimal PurchaseUnitPrice,decimal PurchaseTaxPercent,decimal PurchaseTotal);
