using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class ApplicationUser : IdentityUser
{
    [MaxLength(200)] public string FullName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    [MaxLength(30)] public string Theme { get; set; } = "ocean";
    public ICollection<ScreenPermission> ScreenPermissions { get; set; } = new List<ScreenPermission>();
}
