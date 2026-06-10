namespace MagdyPOS.Models;

public sealed class AdminDashboardViewModel
{
    public DateTime Date { get; init; }
    public double TodaySales { get; init; }
    public int TodayOrdersCount { get; init; }
    public double TotalMaterialsQuantity { get; init; }
    public int LowStockMaterialsCount { get; init; }
    public IReadOnlyList<DashboardSalesPointViewModel> SalesLast7Days { get; init; } = Array.Empty<DashboardSalesPointViewModel>();
    public IReadOnlyList<DashboardTopProductViewModel> TopProductsToday { get; init; } = Array.Empty<DashboardTopProductViewModel>();
}

public sealed class DashboardSalesPointViewModel
{
    public string Label { get; init; } = string.Empty;
    public double Sales { get; init; }
}

public sealed class DashboardTopProductViewModel
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int QuantitySold { get; init; }
    public double Revenue { get; init; }
    public bool IsLowStock { get; init; }
}
