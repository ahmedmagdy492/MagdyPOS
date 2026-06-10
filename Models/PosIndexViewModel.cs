namespace MagdyPOS.Models;

public record PosProductRow(string Id, string Name, string Category, double Price, string Icon);

public sealed class PosProductsGridViewModel
{
    public IReadOnlyList<PosProductRow> Products { get; init; } = Array.Empty<PosProductRow>();
    public string Query { get; init; } = "";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;
    public int TotalPages { get; init; } = 1;
    public int TotalCount { get; init; }
}

public sealed class PosIndexViewModel
{
    public PosProductsGridViewModel ProductsGrid { get; init; } = new();

    public string CashierName { get; init; } = "";

    public string ShiftLabel { get; init; } = "وردية: صباحية";

    public double TaxRate { get; init; } = 0.0;

    public string StoreName { get; init; } = string.Empty;

    public string StoreAddress { get; init; } = string.Empty;

    public string StorePhone { get; init; } = string.Empty;
}
