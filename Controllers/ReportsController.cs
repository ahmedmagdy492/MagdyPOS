using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace MagdyPOS.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public sealed class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReportsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Stock([FromQuery] string? q, [FromQuery] bool lowOnly = false, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 20)
    {
        ViewData["Title"] = "تقارير المخزون";

        if (pageNo < 1) pageNo = 1;
        if (pageSize < 1) pageSize = 20;
        q = (q ?? string.Empty).Trim();

        var baseQuery = _context.Materials
            .AsNoTracking()
            .Include(m => m.Unit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            baseQuery = baseQuery.Where(m => m.Name.Contains(q) || m.Id.Contains(q));
        }

        if (lowOnly)
        {
            baseQuery = baseQuery.Where(m => m.Quantity <= m.AlertLimit);
        }

        var totalCount = await baseQuery.CountAsync().ConfigureAwait(false);
        ViewBag.TotalCount = totalCount;

        var items = await baseQuery
            .OrderBy(m => m.Name)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new StockSummaryItemViewModel
            {
                Id = m.Id,
                Name = m.Name,
                Quantity = m.Quantity,
                AlertLimit = m.AlertLimit,
                UnitName = m.Unit.Name,
                UnitSymbol = m.Unit.Symbol,
            })
            .ToListAsync()
            .ConfigureAwait(false);

        var lowStockCount = await _context.Materials
            .AsNoTracking()
            .CountAsync(m => m.Quantity <= m.AlertLimit)
            .ConfigureAwait(false);

        var vm = new StockReportViewModel
        {
            TotalMaterialsCount = await _context.Materials.AsNoTracking().CountAsync().ConfigureAwait(false),
            LowStockMaterialsCount = lowStockCount,
            Materials = items,
        };

        ViewBag.Query = q;
        ViewBag.LowOnly = lowOnly;

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Movements([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] StockItemType? itemType, [FromQuery] StockMovementKind? kind, [FromQuery] string? q, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 20)
    {
        ViewData["Title"] = "حركات المخزون";

        if (pageNo < 1) pageNo = 1;
        if (pageSize < 1) pageSize = 20;

        if (from is null) from = DateTime.Now.AddDays(-7).Date;
        if (to is null) to = DateTime.Now.Date.AddDays(1).AddTicks(-1);
        if (from > to)
        {
            ViewBag.DateFilterError = "تاريخ البداية بعد تاريخ النهاية";
            return View(new StockMovementsReportViewModel());
        }

        q = (q ?? string.Empty).Trim();

        var query = _context.StockMovements.AsNoTracking().AsQueryable();

        query = query.Where(m => m.CreatedAt >= from && m.CreatedAt <= to);

        if (itemType.HasValue)
        {
            query = query.Where(m => m.ItemType == itemType.Value);
        }

        if (kind.HasValue)
        {
            query = query.Where(m => m.Kind == kind.Value);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(m => m.ItemName.Contains(q) || m.ItemId.Contains(q) || (m.UserName != null && m.UserName.Contains(q)));
        }

        var totalCount = await query.CountAsync().ConfigureAwait(false);
        ViewBag.TotalCount = totalCount;

        var movements = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new StockMovementListItemViewModel
            {
                Id = m.Id,
                CreatedAt = m.CreatedAt,
                ItemType = m.ItemType,
                ItemId = m.ItemId,
                ItemName = m.ItemName,
                Kind = m.Kind,
                QuantityDelta = m.QuantityDelta,
                QuantityBefore = m.QuantityBefore,
                QuantityAfter = m.QuantityAfter,
                Reason = m.Reason,
                ReferenceType = m.ReferenceType,
                ReferenceId = m.ReferenceId,
                UserName = m.UserName,
            })
            .ToListAsync()
            .ConfigureAwait(false);

        ViewBag.From = from.Value.ToString("yyyy-MM-dd");
        ViewBag.To = to.Value.ToString("yyyy-MM-dd");
        ViewBag.ItemType = itemType;
        ViewBag.Kind = kind;
        ViewBag.Query = q;

        return View(new StockMovementsReportViewModel { Movements = movements });
    }

    // JSON endpoints (fast server-side pagination)
    [HttpGet("/api/reports/stock-movements")]
    public async Task<IActionResult> StockMovementsData([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] StockItemType? itemType, [FromQuery] StockMovementKind? kind, [FromQuery] string? q, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 50)
    {
        if (pageNo < 1) pageNo = 1;
        if (pageSize < 1) pageSize = 50;
        if (from is null) from = DateTime.Now.AddDays(-7).Date;
        if (to is null) to = DateTime.Now.Date.AddDays(1).AddTicks(-1);
        if (from > to) return BadRequest(new { message = "Invalid date range" });

        q = (q ?? string.Empty).Trim();

        var query = _context.StockMovements.AsNoTracking().AsQueryable();
        query = query.Where(m => m.CreatedAt >= from && m.CreatedAt <= to);
        if (itemType.HasValue) query = query.Where(m => m.ItemType == itemType.Value);
        if (kind.HasValue) query = query.Where(m => m.Kind == kind.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(m => m.ItemName.Contains(q) || m.ItemId.Contains(q) || (m.UserName != null && m.UserName.Contains(q)));
        }

        var totalCount = await query.CountAsync().ConfigureAwait(false);

        var data = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenByDescending(m => m.Id)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.Id,
                m.CreatedAt,
                m.ItemType,
                m.ItemId,
                m.ItemName,
                m.Kind,
                m.QuantityDelta,
                m.QuantityBefore,
                m.QuantityAfter,
                m.Reason,
                m.ReferenceType,
                m.ReferenceId,
                m.UserName,
            })
            .ToListAsync()
            .ConfigureAwait(false);

        return Ok(new { pageNo, pageSize, totalCount, data });
    }

    [HttpGet]
    public async Task<IActionResult> Revenue([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        ViewData["Title"] = "تقرير الإيراد (المبيعات - المصروفات)";

        if (from is null) from = DateTime.Now.AddDays(-7).Date;
        if (to is null) to = DateTime.Now.Date.AddDays(1).AddTicks(-1);
        if (from > to)
        {
            ViewBag.DateFilterError = "تاريخ البداية بعد تاريخ النهاية";
            return View(new RevenueReportViewModel { From = DateTime.Now.Date, To = DateTime.Now.Date });
        }

        var salesWithin = _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= from && o.OrderDate <= to);

        var salesGross = await salesWithin
            .SelectMany(o => o.OrderDetails)
            .SumAsync(d => (double?)(d.UnitPrice * d.Quantity))
            .ConfigureAwait(false) ?? 0;

        var salesDiscount = await salesWithin
            .SumAsync(o => (double?)o.Discount)
            .ConfigureAwait(false) ?? 0;

        var expensesQuery = _context.Expenses
            .AsNoTracking()
            .Include(e => e.Material)
            .Where(e => e.CreatedAt >= from && e.CreatedAt <= to);

        var expensesTotal = await expensesQuery
            .SumAsync(e => (double?)e.Amount)
            .ConfigureAwait(false) ?? 0;

        var expenses = await expensesQuery
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(200)
            .ToListAsync()
            .ConfigureAwait(false);

        ViewBag.From = from.Value.ToString("yyyy-MM-dd");
        ViewBag.To = to.Value.ToString("yyyy-MM-dd");

        return View(new RevenueReportViewModel
        {
            From = from.Value,
            To = to.Value,
            SalesGross = salesGross,
            SalesDiscount = salesDiscount,
            ExpensesTotal = expensesTotal,
            Expenses = expenses,
        });
    }

    [HttpGet]
    public async Task<IActionResult> DailySummary([FromQuery] DateTime? date = null)
    {
        var target = (date ?? DateTime.Today).Date;
        var start = target;
        var end = start.AddDays(1);

        ViewData["Title"] = $"ملخص اليوم ({target:yyyy-MM-dd})";

        var ordersWithin = _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= start && o.OrderDate < end);

        var ordersCount = await ordersWithin.CountAsync().ConfigureAwait(false);

        var salesGross = await ordersWithin
            .SelectMany(o => o.OrderDetails)
            .SumAsync(d => (double?)(d.UnitPrice * d.Quantity))
            .ConfigureAwait(false) ?? 0;

        var salesDiscount = await ordersWithin
            .SumAsync(o => (double?)o.Discount)
            .ConfigureAwait(false) ?? 0;

        var expensesWithin = _context.Expenses
            .AsNoTracking()
            .Include(e => e.Material)
            .Where(e => e.CreatedAt >= start && e.CreatedAt < end);

        var expensesTotal = await expensesWithin
            .SumAsync(e => (double?)e.Amount)
            .ConfigureAwait(false) ?? 0;

        var purchasesQuery = expensesWithin.Where(e => e.MaterialId != null);

        var purchasesTotal = await purchasesQuery
            .SumAsync(e => (double?)e.Amount)
            .ConfigureAwait(false) ?? 0;

        var purchases = await purchasesQuery
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(200)
            .ToListAsync()
            .ConfigureAwait(false);

        return View(new DailySummaryReportViewModel
        {
            Date = target,
            Start = start,
            End = end,
            OrdersCount = ordersCount,
            SalesGross = salesGross,
            SalesDiscount = salesDiscount,
            PurchasesTotal = purchasesTotal,
            ExpensesTotal = expensesTotal,
            Purchases = purchases,
        });
    }

    [HttpGet]
    public async Task<IActionResult> DailySummaryExport([FromQuery] DateTime? date = null)
    {
        var target = (date ?? DateTime.Today).Date;
        var start = target;
        var end = start.AddDays(1);

        var ordersWithin = _context.Orders
            .AsNoTracking()
            .Where(o => o.OrderDate >= start && o.OrderDate < end);

        var ordersCount = await ordersWithin.CountAsync().ConfigureAwait(false);

        var salesGross = await ordersWithin
            .SelectMany(o => o.OrderDetails)
            .SumAsync(d => (double?)(d.UnitPrice * d.Quantity))
            .ConfigureAwait(false) ?? 0;

        var salesDiscount = await ordersWithin
            .SumAsync(o => (double?)o.Discount)
            .ConfigureAwait(false) ?? 0;

        var expensesWithin = _context.Expenses
            .AsNoTracking()
            .Include(e => e.Material)
            .Where(e => e.CreatedAt >= start && e.CreatedAt < end);

        var expensesTotal = await expensesWithin
            .SumAsync(e => (double?)e.Amount)
            .ConfigureAwait(false) ?? 0;

        var purchasesQuery = expensesWithin.Where(e => e.MaterialId != null);
        var purchasesTotal = await purchasesQuery
            .SumAsync(e => (double?)e.Amount)
            .ConfigureAwait(false) ?? 0;

        var purchases = await purchasesQuery
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .ToListAsync()
            .ConfigureAwait(false);

        var salesNet = Math.Max(0, salesGross - salesDiscount);
        var revenueNet = salesNet - expensesTotal;

        // CSV is the simplest "Excel export" (Excel opens it directly).
        // Add BOM so Arabic text displays correctly in Excel.
        var sb = new StringBuilder();
        sb.AppendLine($"Date,{target:yyyy-MM-dd}");
        sb.AppendLine();

        sb.AppendLine("Sales Summary");
        sb.AppendLine("OrdersCount,SalesGross,SalesDiscount,SalesNet");
        sb.AppendLine($"{ordersCount},{salesGross:0.00},{salesDiscount:0.00},{salesNet:0.00}");
        sb.AppendLine();

        sb.AppendLine("Expenses Summary");
        sb.AppendLine("PurchasesTotal,ExpensesTotal,RevenueNet");
        sb.AppendLine($"{purchasesTotal:0.00},{expensesTotal:0.00},{revenueNet:0.00}");
        sb.AppendLine();

        sb.AppendLine("Purchases (Material-linked expenses)");
        sb.AppendLine("Id,CreatedAt,Title,Amount,MaterialId,MaterialName,MaterialQuantity,Notes");
        foreach (var p in purchases)
        {
            var createdAt = p.CreatedAt.ToString("yyyy-MM-dd HH:mm");
            var title = Csv(p.Title);
            var notes = Csv(p.Notes);
            var materialId = Csv(p.MaterialId);
            var materialName = Csv(p.Material?.Name);
            var qty = p.MaterialQuantity.HasValue ? p.MaterialQuantity.Value.ToString("0.###") : "";

            sb.AppendLine($"{p.Id},{createdAt},{title},{p.Amount:0.00},{materialId},{materialName},{qty},{notes}");
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        var fileName = $"daily-summary-{target:yyyyMMdd}.csv";
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var v = value.Replace("\"", "\"\"");
        return $"\"{v}\"";
    }
}

