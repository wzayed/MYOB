using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MYOB.Models;

namespace MYOB.Pages.Profile;

[Authorize]
public class ThemeModel(UserManager<ApplicationUser> users) : PageModel
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { "ocean", "classic", "emerald", "burgundy", "dark" };

    public IActionResult OnGet() => RedirectToPage("/Index");

    public async Task<IActionResult> OnPostAsync(string theme, string? returnUrl)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge();
        if (!Allowed.Contains(theme)) theme = "ocean";
        user.Theme = theme.ToLowerInvariant();
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded) return RedirectToPage("/Error");
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Page("/Index")!);
    }
}
