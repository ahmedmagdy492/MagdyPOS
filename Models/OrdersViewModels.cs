namespace MagdyPOS.Models;

public sealed class OrderListItemViewModel
{
    public string OrderId { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int PaymentMethod { get; init; }
    public double Discount { get; init; }
    public string Notes { get; init; }
    public double Tips { get; set; }
    public int ItemsCount { get; init; }
    public double Subtotal { get; init; }
    public double Total { get; init; }
}

public sealed class OrderDetailsViewModel
{
    public string OrderId { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public string UserName { get; init; } = string.Empty;
    public int PaymentMethod { get; init; }
    public double Discount { get; init; }
    public string Notes { get; init; }
    public double Tips { get; set; }
    public double Subtotal { get; init; }
    public double Total { get; init; }
    public IReadOnlyList<OrderDetailsItemViewModel> Items { get; init; } = Array.Empty<OrderDetailsItemViewModel>();
}

public sealed class OrderDetailsItemViewModel
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public double UnitPrice { get; init; }
    public int Quantity { get; init; }
    public double LineTotal { get; init; }
}

