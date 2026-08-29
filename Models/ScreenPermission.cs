using System.ComponentModel.DataAnnotations;
namespace MYOB.Models;
public class ScreenPermission
{
    public int Id { get; set; }
    [Required] public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    [Required, MaxLength(100)] public string ScreenKey { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
}
