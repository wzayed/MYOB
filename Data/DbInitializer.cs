using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MYOB.Models;
namespace MYOB.Data;
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Admin")) await roles.CreateAsync(new IdentityRole("Admin"));
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var email = config["SeedAdmin:Email"];
        var password = config["SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password)) return;
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, FullName = "مدير النظام", IsAdmin = true };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
        }
        if (!await users.IsInRoleAsync(user, "Admin")) await users.AddToRoleAsync(user, "Admin");
    }
}
