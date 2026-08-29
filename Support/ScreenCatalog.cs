namespace MYOB.Support;
public sealed record ScreenDefinition(string Key, string Name, string Group, string Path);
public static class ScreenCatalog
{
    public const string Suppliers = "settings.suppliers";
    public const string Materials = "settings.materials";
    public const string Customers = "settings.customers";
    public const string Units = "settings.units";
    public const string PaymentMethods = "settings.payment-methods";
    public const string SupplierReceipts = "operations.supplier-receipts";
    public const string SupplierPayments = "operations.supplier-payments";
    public const string CustomerDeliveries = "operations.customer-deliveries";
    public const string CustomerPayments = "operations.customer-payments";
    public const string Users = "settings.users";
    public const string AuditTrail = "reports.audit-trail";
    public static readonly IReadOnlyList<ScreenDefinition> All =
    [
        new(Suppliers, "الموردون", "الإعدادات", "/Settings/Suppliers"),
        new(Materials, "الخامات", "الإعدادات", "/Settings/Materials"),
        new(Customers, "إعداد العملاء", "الإعدادات", "/Settings/Customers"),
        new(Units, "إعداد وحدات القياس", "الإعدادات", "/Settings/Units"),
        new(PaymentMethods, "إعداد طرق الدفع", "الإعدادات", "/Settings/PaymentMethods"),
        new(SupplierReceipts, "إستلام بضاعة من مورد", "العمليات", "/Operations/SupplierReceipts"),
        new(SupplierPayments, "دفع إلى مورد", "العمليات", "/Operations/SupplierPayments"),
        new(CustomerDeliveries, "توريد بضاعة لعميل", "العمليات", "/Operations/CustomerDeliveries"),
        new(CustomerPayments, "قبض من عميل", "العمليات", "/Operations/CustomerPayments"),
        new(Users, "المستخدمون والصلاحيات", "الإعدادات", "/Settings/Users"),
        new(AuditTrail, "سجل المراجعة", "التقارير", "/Reports/AuditTrail")
    ];
    public static ScreenDefinition? FromPath(string path) => All.OrderByDescending(x => x.Path.Length)
        .FirstOrDefault(x => path.StartsWith(x.Path, StringComparison.OrdinalIgnoreCase));
}
