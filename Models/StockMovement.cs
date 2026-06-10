using System.ComponentModel.DataAnnotations;

namespace MagdyPOS.Models;

public enum StockItemType
{
    Material = 1,
    Product = 2,
}

public enum StockMovementKind
{
    In = 1,
    Out = 2,
    Adjust = 3,
}

public sealed class StockMovement
{
    public long Id { get; set; }

    public StockItemType ItemType { get; set; } = StockItemType.Material;

    [MaxLength(100)]
    public string ItemId { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ItemName { get; set; } = string.Empty;

    public StockMovementKind Kind { get; set; } = StockMovementKind.Adjust;

    /// <summary>
    /// Positive means stock-in, negative means stock-out.
    /// </summary>
    public double QuantityDelta { get; set; }

    public double? QuantityBefore { get; set; }
    public double? QuantityAfter { get; set; }

    [MaxLength(400)]
    public string Reason { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ReferenceType { get; set; }

    [MaxLength(100)]
    public string ReferenceId { get; set; }

    [MaxLength(450)]
    public string UserId { get; set; }

    [MaxLength(200)]
    public string UserName { get; set; }

    public DateTime CreatedAt { get; set; }
}

