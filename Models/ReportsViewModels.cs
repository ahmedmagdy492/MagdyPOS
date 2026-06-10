namespace MagdyPOS.Models;

public sealed class StockSummaryItemViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string UnitName { get; init; } = string.Empty;
    public string UnitSymbol { get; init; } = string.Empty;
    public double Quantity { get; init; }
    public double AlertLimit { get; init; }
    public bool IsLowStock => Quantity <= AlertLimit;
}

public sealed class StockReportViewModel
{
    public int TotalMaterialsCount { get; init; }
    public int LowStockMaterialsCount { get; init; }
    public IReadOnlyList<StockSummaryItemViewModel> Materials { get; init; } = Array.Empty<StockSummaryItemViewModel>();
}

public sealed class StockMovementListItemViewModel
{
    public long Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public StockItemType ItemType { get; init; }
    public string ItemId { get; init; } = string.Empty;
    public string ItemName { get; init; } = string.Empty;
    public StockMovementKind Kind { get; init; }
    public double QuantityDelta { get; init; }
    public double? QuantityBefore { get; init; }
    public double? QuantityAfter { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? ReferenceType { get; init; }
    public string? ReferenceId { get; init; }
    public string? UserName { get; init; }
}

public sealed class StockMovementsReportViewModel
{
    public IReadOnlyList<StockMovementListItemViewModel> Movements { get; init; } = Array.Empty<StockMovementListItemViewModel>();
}

public sealed class RevenueReportViewModel
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }

    public double SalesGross { get; init; }
    public double SalesDiscount { get; init; }
    public double SalesNet => Math.Max(0, SalesGross - SalesDiscount);

    public double ExpensesTotal { get; init; }
    public double RevenueNet => SalesNet - ExpensesTotal;

    public IReadOnlyList<Expense> Expenses { get; init; } = Array.Empty<Expense>();
}

public sealed class DailySummaryReportViewModel
{
    public DateTime Date { get; init; }
    public DateTime Start { get; init; }
    public DateTime End { get; init; }

    public int OrdersCount { get; init; }
    public double SalesGross { get; init; }
    public double SalesDiscount { get; init; }
    public double SalesNet => Math.Max(0, SalesGross - SalesDiscount);

    /// <summary>
    /// Purchases = expenses linked to a material (MaterialId not null).
    /// </summary>
    public double PurchasesTotal { get; init; }

    public double ExpensesTotal { get; init; }

    public IReadOnlyList<Expense> Purchases { get; init; } = Array.Empty<Expense>();
}

