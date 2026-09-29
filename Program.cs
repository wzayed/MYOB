using Microsoft.EntityFrameworkCore;
using MYOB.Data;
using MYOB.Models;
using MYOB.Services;
using MYOB.Support;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;

var applicationCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
applicationCulture.DateTimeFormat.ShortDatePattern = "yyyy/MM/dd";
applicationCulture.DateTimeFormat.DateSeparator = "/";
CultureInfo.DefaultThreadCurrentCulture = applicationCulture;
CultureInfo.DefaultThreadCurrentUICulture = applicationCulture;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddHttpContextAccessor();
var keysPath = builder.Configuration["DataProtection:KeysPath"] ?? Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath)).SetApplicationName("MYOB");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddRoles<Microsoft.AspNetCore.Identity.IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
    // Renew on every authenticated request so the full hour starts at the last use.
    options.Events.OnCheckSlidingExpiration = context =>
    {
        context.ShouldRenew = true;
        return Task.CompletedTask;
    };
    // Require a fresh login instead of accepting existing cookies with the old lifetime.
    options.Cookie.Name = ".MYOB.Identity.OneHour";
});
builder.Services.AddAuthorization(options =>
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAuditReportService, AuditReportService>();
builder.Services.AddScoped<ITransactionService,TransactionService>();
builder.Services.AddScoped<IFinancialReportService,FinancialReportService>();
builder.Services.AddScoped<ScreenPermissionPageFilter>();
builder.Services.AddRazorPages(options => options.Conventions.ConfigureFilter(
    new Microsoft.AspNetCore.Mvc.ServiceFilterAttribute(typeof(ScreenPermissionPageFilter))));
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.UseMigrationsEndPoint();
else { app.UseExceptionHandler("/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
await DbInitializer.InitializeAsync(app.Services, app.Environment.IsDevelopment());
app.Run();
