using System.ComponentModel.DataAnnotations;

namespace MagdyPOS.Models;

public sealed class OrganizationProfile
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Address { get; set; } = string.Empty;
}
