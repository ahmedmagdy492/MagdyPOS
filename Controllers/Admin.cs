using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagdyPOS.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? date = null)
    {
        ViewData["Title"] = "لوحة التحكم";

        var targetDate = (date ?? DateTime.Today).Date;
        var start = targetDate;
        var end = start.AddDays(1);

        var todayOrderLines = _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= start && o.OrderDate < end)
            .SelectMany(o => o.OrderDetails, (o, d) => new
            {
                d.ProductId,
                d.Quantity,
                d.UnitPrice,
                o.Discount
            });

        var todaySalesBeforeDiscount = await todayOrderLines
            .SumAsync(x => (double?)(x.UnitPrice * x.Quantity))
            .ConfigureAwait(false) ?? 0;

        var todayDiscount = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= start && o.OrderDate < end)
            .SumAsync(o => (double?)o.Discount)
            .ConfigureAwait(false) ?? 0;

        var todaySales = todaySalesBeforeDiscount - todayDiscount;
        if (todaySales < 0)
        {
            todaySales = 0;
        }

        var todayOrdersCount = await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.OrderDate >= start && o.OrderDate < end)
            .ConfigureAwait(false);

        var totalMaterialsQuantity = await _context.Materials
            .AsNoTracking()
            .SumAsync(m => (double?)m.Quantity)
            .ConfigureAwait(false) ?? 0;

        var lowStockMaterialsCount = await _context.Materials
            .AsNoTracking()
            .CountAsync(m => m.Quantity <= m.AlertLimit)
            .ConfigureAwait(false);

        var chartStart = targetDate.AddDays(-6);
        var chartEnd = targetDate.AddDays(1);

        var salesByDate = await _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= chartStart && o.OrderDate < chartEnd)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Gross = g.SelectMany(o => o.OrderDetails).Sum(d => d.UnitPrice * d.Quantity),
                Discount = g.Sum(o => o.Discount)
            })
            .ToListAsync()
            .ConfigureAwait(false);

        var salesMap = salesByDate.ToDictionary(x => x.Date, x => Math.Max(0, x.Gross - x.Discount));
        var salesLast7Days = Enumerable.Range(0, 7)
            .Select(offset =>
            {
                var d = chartStart.AddDays(offset);
                salesMap.TryGetValue(d.Date, out var val);
                return new DashboardSalesPointViewModel
                {
                    Label = d.ToString("ddd", new System.Globalization.CultureInfo("ar-EG")),
                    Sales = val
                };
            })
            .ToList();

        var topProductsRaw = await _context.OrderDetails
            .AsNoTracking()
            .Where(d => d.Order.OrderDate >= start && d.Order.OrderDate < end)
            .GroupBy(d => new { d.ProductId, d.Product.Name })
            .Select(g => new
            {
                g.Key.ProductId,
                ProductName = g.Key.Name,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.Revenue)
            .Take(10)
            .ToListAsync()
            .ConfigureAwait(false);

        var topProductIds = topProductsRaw.Select(p => p.ProductId).ToList();
        var lowStockProductIds = await _context.ProductMaterials
            .AsNoTracking()
            .Where(pm => topProductIds.Contains(pm.ProductId))
            .Where(pm => pm.Material.Quantity <= pm.Material.AlertLimit)
            .Select(pm => pm.ProductId)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false);
        var lowStockSet = lowStockProductIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var vm = new AdminDashboardViewModel
        {
            Date = targetDate,
            TodaySales = todaySales,
            TodayOrdersCount = todayOrdersCount,
            TotalMaterialsQuantity = totalMaterialsQuantity,
            LowStockMaterialsCount = lowStockMaterialsCount,
            SalesLast7Days = salesLast7Days,
            TopProductsToday = topProductsRaw.Select(p => new DashboardTopProductViewModel
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                QuantitySold = p.QuantitySold,
                Revenue = p.Revenue,
                IsLowStock = lowStockSet.Contains(p.ProductId)
            }).ToList()
        };

        return View(vm);
    }

    public IActionResult Products()
    {
        return RedirectToAction("Index", "Products");
    }
}
