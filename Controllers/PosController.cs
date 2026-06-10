using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MagdyPOS.Helpers;

namespace MagdyPOS.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Sales}")]
public class PosController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public PosController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string q, int page = 1, int pageSize = 12)
    {
        ViewData["Title"] = "نقطة البيع";
        var user = await _userManager.GetUserAsync(User).ConfigureAwait(false);
        var cashier = user?.FullName;
        if (string.IsNullOrWhiteSpace(cashier))
        {
            cashier = user?.Email ?? User.Identity?.Name ?? "الكاشير";
        }

        var grid = await BuildProductsGridAsync(q, page, pageSize).ConfigureAwait(false);
        var orgProfile = await _context.OrganizationProfiles
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        var vm = new PosIndexViewModel
        {
            ProductsGrid = grid,
            CashierName = cashier,
            ShiftLabel = Utils.GetShiftLabel(DateTime.Now),
            StoreName = orgProfile?.Name ?? string.Empty,
            StoreAddress = orgProfile?.Address ?? string.Empty,
            StorePhone = orgProfile?.Phone ?? string.Empty
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ProductsGrid(string? q, int page = 1, int pageSize = 12)
    {
        var grid = await BuildProductsGridAsync(q, page, pageSize).ConfigureAwait(false);
        return PartialView("_ProductsGrid", grid);
    }

    private async Task<PosProductsGridViewModel> BuildProductsGridAsync(string q, int page, int pageSize)
    {
        var query = (q ?? string.Empty).Trim();
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 6 or > 60 ? 12 : pageSize;

        var baseQuery = _context.Products
            .AsNoTracking()
            .Include(p => p.Unit)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            baseQuery = baseQuery.Where(p =>
                p.Name.Contains(query) ||
                p.Id.Contains(query) ||
                p.Unit.Name.Contains(query));
        }

        var total = await baseQuery.CountAsync().ConfigureAwait(false);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        if (page > totalPages) page = totalPages;

        var products = await baseQuery
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PosProductRow(
                p.Id,
                p.Name,
                p.Unit.Name,
                p.Price,
                "📦"))
            .ToListAsync()
            .ConfigureAwait(false);

        return new PosProductsGridViewModel
        {
            Products = products,
            Query = query,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalCount = total,
        };
    }
}
