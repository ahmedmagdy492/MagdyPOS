using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MagdyPOS.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Sales}")]
public class OrdersController : Controller
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery]DateTime? from, [FromQuery]DateTime? to, [FromQuery]int pageNo = 1, [FromQuery]int pageSize = 20)
    {
        if(from == null)
            from = DateTime.Now.AddDays(-1);

        if (to == null)
            to = DateTime.Now;

        if (User.IsInRole(AppRoles.Admin))
        {
            if(from > to)
            {
                ViewBag.DateFilterError = "تاريخ البداية بعد تاريخ النهاية";
                return View(new List<OrderListItemViewModel>());
            }

            ViewData["Title"] = "الطلبات";
            var allOrders = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .Where(o => o.OrderDate >= from && o.OrderDate <= to)
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListItemViewModel
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                UserName = (o.User.FullName ?? o.User.Email ?? o.User.UserName) ?? "—",
                PaymentMethod = o.PaymentMethod,
                Discount = o.Discount,
                Notes = o.Notes,
                Tips = o.Tips,
                ItemsCount = o.OrderDetails.Sum(d => d.Quantity),
                Subtotal = o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity),
                Total = o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity) - o.Discount,
            })
            .ToListAsync()
            .ConfigureAwait(false);

            return View(allOrders);
        }

        ViewData["Title"] = "طلباتي";
        var userId = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier);

        var orders = await _context.Orders
            .Where(o => o.UserId == userId.Value)
            .AsNoTracking()
            .Include(o => o.OrderDetails)
            .OrderByDescending(o => o.OrderDate)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderListItemViewModel
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                UserName = (o.User.FullName ?? o.User.Email ?? o.User.UserName) ?? "—",
                PaymentMethod = o.PaymentMethod,
                Discount = o.Discount,
                Notes = o.Notes,
                ItemsCount = o.OrderDetails.Sum(d => d.Quantity),
                Subtotal = o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity),
                Total = o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity) - o.Discount,
            })
            .ToListAsync()
            .ConfigureAwait(false);

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .Where(o => o.OrderId == id)
            .Select(o => new OrderDetailsViewModel
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                UserName = (o.User.FullName ?? o.User.Email ?? o.User.UserName) ?? "—",
                PaymentMethod = o.PaymentMethod,
                Discount = o.Discount,
                Notes = o.Notes,
                Tips = o.Tips,
                Subtotal = o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity),
                Total = o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity) - o.Discount,
                Items = o.OrderDetails
                    .OrderBy(d => d.Product.Name)
                    .Select(d => new OrderDetailsItemViewModel
                    {
                        ProductId = d.ProductId,
                        ProductName = d.Product.Name,
                        UnitPrice = d.UnitPrice,
                        Quantity = d.Quantity,
                        LineTotal = d.UnitPrice * d.Quantity,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (order is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"تفاصيل الطلب {order.OrderNumber}";
        return View(order);
    }

    [HttpPost("Orders/Submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitOrderRequest request)
    {
        if (request is null || request.Items.Count == 0)
        {
            return BadRequest(new { message = "لا يمكن حفظ طلب بدون أصناف." });
        }

        var invalidLine = request.Items.FirstOrDefault(i => string.IsNullOrWhiteSpace(i.ProductId) || i.Quantity <= 0);
        if (invalidLine is not null)
        {
            return BadRequest(new { message = "بيانات الأصناف غير صحيحة." });
        }

        var productIds = request.Items
            .Select(i => i.ProductId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var products = await _context.Products
            .Include(p => p.ProductMaterials)
            .ThenInclude(pm => pm.Material)
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync()
            .ConfigureAwait(false);

        if (products.Count != productIds.Count)
        {
            return BadRequest(new { message = "يوجد صنف غير موجود ضمن الطلب." });
        }

        var productById = products.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);
        var totalMaterialConsumption = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in request.Items)
        {
            var product = productById[item.ProductId.Trim()];
            foreach (var materialLink in product.ProductMaterials)
            {
                var requiredQty = materialLink.Quantity * item.Quantity;
                if (requiredQty <= 0) continue;

                if (!totalMaterialConsumption.TryGetValue(materialLink.MaterialId, out var accumulated))
                {
                    accumulated = 0;
                }

                totalMaterialConsumption[materialLink.MaterialId] = accumulated + requiredQty;
            }
        }

        var materialIds = totalMaterialConsumption.Keys.ToList();
        var materials = await _context.Materials
            .Where(m => materialIds.Contains(m.Id))
            .ToListAsync()
            .ConfigureAwait(false);

        if (materials.Count != materialIds.Count)
        {
            return BadRequest(new { message = "يوجد خامة مرتبطة بالمنتج وغير موجودة." });
        }

        var insufficient = materials
            .Where(m => m.Quantity < totalMaterialConsumption[m.Id])
            .Select(m => m.Name)
            .ToList();

        if (insufficient.Count > 0)
        {
            return BadRequest(new { message = $"الكمية غير كافية في المخزون للخامات: {string.Join("، ", insufficient)}" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        await using var trx = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            var order = new Order
            {
                OrderId = Guid.NewGuid().ToString("N"),
                OrderNumber = await GenerateOrderNumberAsync().ConfigureAwait(false),
                OrderDate = DateTime.UtcNow,
                UserId = userId,
                PaymentMethod = request.PaymentMethod,
                Discount = request.Discount,
                Notes = request.Notes?.Trim(),
                Tips = request.Tips,
                OrderDetails = request.Items.Select(i =>
                {
                    var product = productById[i.ProductId.Trim()];
                    return new OrderDetail
                    {
                        ProductId = product.Id,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice > 0 ? i.UnitPrice : product.Price,
                    };
                }).ToList(),
            };

            _context.Orders.Add(order);

            foreach (var material in materials)
            {
                var before = material.Quantity;
                var consumed = totalMaterialConsumption[material.Id];
                material.Quantity -= consumed;

                _context.StockMovements.Add(new StockMovement
                {
                    ItemType = StockItemType.Material,
                    ItemId = material.Id,
                    ItemName = material.Name,
                    Kind = StockMovementKind.Out,
                    QuantityDelta = -consumed,
                    QuantityBefore = before,
                    QuantityAfter = material.Quantity,
                    Reason = "استهلاك خامات بسبب عملية بيع",
                    ReferenceType = "Order",
                    ReferenceId = order.OrderId,
                    UserId = userId,
                    UserName = User.Identity?.Name,
                    CreatedAt = DateTime.UtcNow,
                });
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
            await trx.CommitAsync().ConfigureAwait(false);

            return Ok(new { message = "تم حفظ الطلب بنجاح.", orderId = order.OrderId, orderNumber = order.OrderNumber });
        }
        catch
        {
            await trx.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task<string> GenerateOrderNumberAsync()
    {
        while (true)
        {
            var candidate = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}";
            var exists = await _context.Orders
                .AsNoTracking()
                .AnyAsync(o => o.OrderNumber == candidate)
                .ConfigureAwait(false);

            if (!exists)
            {
                return candidate;
            }
        }
    }
}

public sealed class SubmitOrderRequest
{
    public int PaymentMethod { get; init; } = 1;
    public double Discount { get; init; }
    public string Notes { get; init; }
    public double Tips { get; set; }
    public List<SubmitOrderItemRequest> Items { get; init; } = new();
}

public sealed class SubmitOrderItemRequest
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public double UnitPrice { get; init; }
}

