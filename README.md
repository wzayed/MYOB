# MYOB Arabic Business Application

Arabic RTL ASP.NET Core Razor Pages foundation targeting .NET 8 and SQL Server.

## Included

- ASP.NET Core Identity with Admin role and screen-level view/add/update/delete permissions.
- Central screen catalog used by both navigation and server-side page access checks.
- Supplier, customer, material, unit-of-measure and payment-method setup modules with standard Index/Create/Edit pages, search, pagination, validation, audit fields and soft delete.
- Supplier receipt/payment and customer delivery/receipt workflows with policy assignment, server-side totals and remaining-balance validation.
- User administration with Admin assignment and per-screen permissions.
- Automatic entity audit history plus explicit login, logout and user-management audit events.
- Arabic audit-trail report with inclusive dates, editable searchable user filter, pagination and untruncated QuestPDF export.
- Initial reviewed EF Core migration: `InitialBusinessSchema`.

## Local setup

1. Configure the SQL Server connection in `ConnectionStrings:DefaultConnection`. The checked-in default uses a project-specific LocalDB database.
2. Store the initial administrator outside source control:

   ```powershell
   dotnet user-secrets set "SeedAdmin:Email" "admin@example.com"
   dotnet user-secrets set "SeedAdmin:Password" "Choose-A-Strong-Password"
   ```

3. Restore tools and packages, then run:

   ```powershell
   dotnet tool restore
   dotnet restore
   dotnet run
   ```

Development startup applies pending migrations and creates the configured initial Admin. Production startup never applies migrations automatically. Review and deploy migrations through the approved release process.

## Architecture

- `Models/`: entities and audit base class
- `Data/`: DbContext, initialization and migrations
- `Pages/Settings/`, `Pages/Operations/`, `Pages/Reports/`: UI areas
- `Services/`: authorization, auditing and report queries
- `DTOs/`: report filter and row models
- `PdfDocuments/`: QuestPDF layouts (database-free)
- `Support/`: screen catalog, actions and page authorization filter

All persisted financial values use `decimal`. Soft-deletable master entities have global query filters, while destructive handlers perform soft deletion.
