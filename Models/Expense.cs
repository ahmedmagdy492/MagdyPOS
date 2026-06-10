using System.ComponentModel.DataAnnotations;

namespace MagdyPOS.Models;

public sealed class Expense
{
    public long Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public double Amount { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Local time for display/filters (consistent with other entities usage).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Optional link to a material purchase/adjustment.
    /// When set + MaterialQuantity > 0, stock will be increased.
    /// </summary>
    [MaxLength(100)]
    public string? MaterialId { get; set; }

    public double? MaterialQuantity { get; set; }

    [MaxLength(450)]
    public string? UserId { get; set; }

    [MaxLength(200)]
    public string? UserName { get; set; }

    public Material? Material { get; set; }
}

