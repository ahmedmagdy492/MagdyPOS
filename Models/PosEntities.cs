namespace MagdyPOS.Models;

public class Unit
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string? Notes { get; set; }

    public ICollection<Material> Materials { get; set; } = new List<Material>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

public class Material
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public double OriginalQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public long UnitId { get; set; }
    public double AlertLimit { get; set; }

    public Unit Unit { get; set; } = null!;
    public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
}

public class Product
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public long UnitId { get; set; }

    public Unit Unit { get; set; } = null!;
    public ICollection<ProductMaterial> ProductMaterials { get; set; } = new List<ProductMaterial>();
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

public class ProductMaterial
{
    public string ProductId { get; set; } = string.Empty;
    public string MaterialId { get; set; } = string.Empty;
    public double Quantity { get; set; }

    public Product Product { get; set; } = null!;
    public Material Material { get; set; } = null!;
}

public enum OrderState
{
    Paid,
    Returned,
    Postponed
}

public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int PaymentMethod { get; set; }
    public double Discount { get; set; }
    public string Notes { get; set; }
    public double Tips { get; set; }

    public OrderState State { get; set; } = OrderState.Paid;


    public ApplicationUser User { get; set; } = null!;
    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

public class OrderDetail
{
    public string OrderId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public double UnitPrice { get; set; }
    public int Quantity { get; set; }

    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
