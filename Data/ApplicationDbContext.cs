using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MYOB.Models;
using System.Text.Json;

namespace MYOB.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor)
        : base(options) => _httpContextAccessor = httpContextAccessor;

    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Customer> Customers=>Set<Customer>(); public DbSet<UnitOfMeasure> Units=>Set<UnitOfMeasure>(); public DbSet<PaymentMethod> PaymentMethods=>Set<PaymentMethod>();
    public DbSet<SupplierReceipt> SupplierReceipts=>Set<SupplierReceipt>(); public DbSet<SupplierPayment> SupplierPayments=>Set<SupplierPayment>();
    public DbSet<CustomerDelivery> CustomerDeliveries=>Set<CustomerDelivery>(); public DbSet<CustomerPayment> CustomerPayments=>Set<CustomerPayment>();
    public DbSet<ScreenPermission> ScreenPermissions => Set<ScreenPermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Supplier>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Material>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Customer>().HasQueryFilter(x=>!x.IsDeleted);builder.Entity<UnitOfMeasure>().HasQueryFilter(x=>!x.IsDeleted);builder.Entity<PaymentMethod>().HasQueryFilter(x=>!x.IsDeleted);
        builder.Entity<SupplierReceipt>().HasQueryFilter(x=>!x.IsDeleted);builder.Entity<SupplierPayment>().HasQueryFilter(x=>!x.IsDeleted);builder.Entity<CustomerDelivery>().HasQueryFilter(x=>!x.IsDeleted);builder.Entity<CustomerPayment>().HasQueryFilter(x=>!x.IsDeleted);
        builder.Entity<Supplier>().Property(x => x.OpeningBalance).HasPrecision(18, 2);
        builder.Entity<Customer>().Property(x=>x.OpeningBalance).HasPrecision(18,2);
        builder.Entity<Material>().HasOne(x=>x.UnitOfMeasure).WithMany(x=>x.Materials).HasForeignKey(x=>x.UnitOfMeasureId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SupplierReceipt>().HasIndex(x=>x.PolicyNumber).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Entity<CustomerDelivery>().HasIndex(x=>x.SupplierReceiptId).IsUnique().HasFilter("[IsDeleted] = 0");
        builder.Entity<SupplierReceipt>().HasOne(x=>x.CustomerDelivery).WithOne(x=>x.SupplierReceipt).HasForeignKey<CustomerDelivery>(x=>x.SupplierReceiptId).OnDelete(DeleteBehavior.Restrict);
        foreach(var type in new[]{typeof(SupplierReceipt),typeof(SupplierPayment),typeof(CustomerDelivery),typeof(CustomerPayment)})
        {
            foreach(var property in type.GetProperties().Where(p=>p.PropertyType==typeof(decimal))) builder.Entity(type).Property(property.Name).HasPrecision(18,2);
        }
        builder.Entity<SupplierReceipt>().Property(x=>x.Date).HasColumnType("date");builder.Entity<SupplierPayment>().Property(x=>x.Date).HasColumnType("date");builder.Entity<CustomerDelivery>().Property(x=>x.Date).HasColumnType("date");builder.Entity<CustomerPayment>().Property(x=>x.Date).HasColumnType("date");
        builder.Entity<SupplierPayment>().HasOne(x=>x.SupplierReceipt).WithMany(x=>x.Payments).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<CustomerPayment>().HasOne(x=>x.CustomerDelivery).WithMany(x=>x.Payments).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ScreenPermission>().HasIndex(x => new { x.UserId, x.ScreenKey }).IsUnique();
        builder.Entity<ScreenPermission>().HasOne(x => x.User).WithMany(x => x.ScreenPermissions)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAuditEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AddAuditEntries()
    {
        var now = DateTimeOffset.UtcNow;
        var context = _httpContextAccessor.HttpContext;
        var principal = context?.User;
        var entries = ChangeTracker.Entries<AuditableEntity>().Where(e => e.State is EntityState.Added or EntityState.Modified).ToList();
        if (context is not null && principal?.Identity?.IsAuthenticated != true && entries.Count > 0)
            throw new UnauthorizedAccessException("لا يمكن تعديل بيانات النظام دون تسجيل الدخول.");
        var userName = principal?.Identity?.Name ?? "النظام";
        var userId = principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var path = context?.Request.Path.Value ?? "System";
        var ip = context?.Connection.RemoteIpAddress?.ToString();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added) { entry.Entity.CreatedAt = now; entry.Entity.CreatedBy = userName; }
            else { entry.Entity.UpdatedAt = now; entry.Entity.UpdatedBy = userName; }
            var oldValues = entry.State == EntityState.Modified
                ? entry.Properties.Where(p => p.IsModified).ToDictionary(p => p.Metadata.Name, p => p.OriginalValue) : null;
            var newValues = entry.Properties.Where(p => entry.State == EntityState.Added || p.IsModified)
                .ToDictionary(p => p.Metadata.Name, p => p.CurrentValue);
            var action = entry.State == EntityState.Added ? "إنشاء"
                : entry.Entity.IsDeleted && entry.Property(nameof(AuditableEntity.IsDeleted)).IsModified ? "حذف" : "تعديل";
            AuditLogs.Add(new AuditLog
            {
                UserId = userId, UserName = userName, Action = action, Screen = path,
                EntityName = entry.Metadata.ClrType.Name, RecordId = entry.Entity.Id.ToString(),
                OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues),
                NewValues = JsonSerializer.Serialize(newValues), IpAddress = ip, OccurredAt = now
            });
        }
    }
}
