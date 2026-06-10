using Microsoft.AspNetCore.Identity;

namespace MagdyPOS.Models;

public class ApplicationUser : IdentityUser
{
    public string? FullName { get; set; }
}
