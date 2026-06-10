namespace MagdyPOS.Models;

public sealed class ProductListItemViewModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double Price { get; init; }
    public long UnitId { get; init; }
    public string UnitName { get; init; } = string.Empty;
    public string UnitSymbol { get; init; } = string.Empty;
    public int MaterialsCount { get; init; }
    public IReadOnlyList<string> MaterialIds { get; init; } = Array.Empty<string>();
    public IReadOnlyList<double> MaterialQuantities { get; init; } = Array.Empty<double>();
}

