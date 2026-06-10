using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagdyPOS.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class ProductsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "المنتجات";

        var products = await _context.Products
            .AsNoTracking()
            .Include(p => p.Unit)
            .Include(p => p.ProductMaterials)
            .OrderBy(p => p.Name)
            .Select(p => new ProductListItemViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                UnitId = p.UnitId,
                UnitName = p.Unit.Name,
                UnitSymbol = p.Unit.Symbol,
                MaterialsCount = p.ProductMaterials.Count,
                MaterialIds = p.ProductMaterials.Select(pm => pm.MaterialId).ToList(),
                MaterialQuantities = p.ProductMaterials.Select(pm => pm.Quantity).ToList(),
            })
            .ToListAsync()
            .ConfigureAwait(false);

        ViewBag.Units = await _context.Units.AsNoTracking().OrderBy(u => u.Name).ToListAsync().ConfigureAwait(false);
        ViewBag.Materials = await _context.Materials.AsNoTracking().OrderBy(m => m.Name).ToListAsync().ConfigureAwait(false);

        return View(products);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateModal(
        [FromForm] string id,
        [FromForm] string name,
        [FromForm] double price,
        [FromForm] long unitId,
        [FromForm] List<string> materialIds,
        [FromForm] List<double> materialQuantities)
    {
        id = (id ?? string.Empty).Trim();
        name = (name ?? string.Empty).Trim();
        materialIds = materialIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList()
            ?? new List<string>();

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "الكود واسم المنتج مطلوبان." });
        }

        var unit = await _context.Units.FindAsync(unitId).ConfigureAwait(false);
        if (unit is null)
        {
            return BadRequest(new { message = "الوحدة غير صحيحة." });
        }

        var exists = await _context.Products.AnyAsync(p => p.Id == id).ConfigureAwait(false);
        if (exists)
        {
            return Conflict(new { message = "هذا الكود موجود بالفعل." });
        }

        if (materialIds.Count > 0)
        {
            var existingMaterialIds = await _context.Materials
                .Where(m => materialIds.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            if (existingMaterialIds.Count != materialIds.Count)
            {
                return BadRequest(new { message = "بعض الخامات المختارة غير موجودة." });
            }
        }

        var recipe = BuildRecipe(materialIds, materialQuantities);
        if (!recipe.IsValid)
        {
            return BadRequest(new { message = recipe.Message });
        }

        var product = new Product
        {
            Id = id,
            Name = name,
            Price = price,
            UnitId = unitId,
        };

        _context.Products.Add(product);

        foreach (var line in recipe.Lines)
        {
            _context.ProductMaterials.Add(new ProductMaterial
            {
                ProductId = id,
                MaterialId = line.MaterialId,
                Quantity = line.Quantity,
            });
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return Json(new
        {
            id = product.Id,
            name = product.Name,
            price = product.Price,
            unitId = unit.Id,
            unitName = unit.Name,
            unitSymbol = unit.Symbol,
            materialsCount = recipe.Lines.Count,
            materialIds = recipe.Lines.Select(x => x.MaterialId).ToList(),
            materialQuantities = recipe.Lines.Select(x => x.Quantity).ToList(),
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditModal(
        [FromForm] string id,
        [FromForm] string name,
        [FromForm] double price,
        [FromForm] long unitId,
        [FromForm] List<string> materialIds,
        [FromForm] List<double> materialQuantities)
    {
        id = (id ?? string.Empty).Trim();
        name = (name ?? string.Empty).Trim();
        materialIds = materialIds?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList()
            ?? new List<string>();

        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { message = "الكود غير صحيح." });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "اسم المنتج مطلوب." });
        }

        var product = await _context.Products
            .Include(p => p.ProductMaterials)
            .FirstOrDefaultAsync(p => p.Id == id)
            .ConfigureAwait(false);

        if (product is null)
        {
            return NotFound(new { message = "المنتج غير موجود." });
        }

        var unit = await _context.Units.FindAsync(unitId).ConfigureAwait(false);
        if (unit is null)
        {
            return BadRequest(new { message = "الوحدة غير صحيحة." });
        }

        if (materialIds.Count > 0)
        {
            var existingMaterialIds = await _context.Materials
                .Where(m => materialIds.Contains(m.Id))
                .Select(m => m.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            if (existingMaterialIds.Count != materialIds.Count)
            {
                return BadRequest(new { message = "بعض الخامات المختارة غير موجودة." });
            }
        }

        var recipe = BuildRecipe(materialIds, materialQuantities);
        if (!recipe.IsValid)
        {
            return BadRequest(new { message = recipe.Message });
        }

        product.Name = name;
        product.Price = price;
        product.UnitId = unitId;

        _context.ProductMaterials.RemoveRange(product.ProductMaterials);
        foreach (var line in recipe.Lines)
        {
            _context.ProductMaterials.Add(new ProductMaterial
            {
                ProductId = id,
                MaterialId = line.MaterialId,
                Quantity = line.Quantity,
            });
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return Json(new
        {
            id = product.Id,
            name = product.Name,
            price = product.Price,
            unitId = unit.Id,
            unitName = unit.Name,
            unitSymbol = unit.Symbol,
            materialsCount = recipe.Lines.Count,
            materialIds = recipe.Lines.Select(x => x.MaterialId).ToList(),
            materialQuantities = recipe.Lines.Select(x => x.Quantity).ToList(),
        });
    }

    private static ProductRecipeParseResult BuildRecipe(List<string> materialIds, List<double> materialQuantities)
    {
        materialIds ??= new List<string>();
        materialQuantities ??= new List<double>();

        if (materialIds.Count != materialQuantities.Count)
        {
            return ProductRecipeParseResult.Error("يجب تحديد كمية لكل خامة مختارة.");
        }

        var lines = new List<ProductRecipeLine>();
        for (var i = 0; i < materialIds.Count; i++)
        {
            var id = (materialIds[i] ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var qty = materialQuantities[i];
            if (qty <= 0)
            {
                return ProductRecipeParseResult.Error("كمية الخامة يجب أن تكون أكبر من صفر.");
            }

            lines.Add(new ProductRecipeLine(id, qty));
        }

        return ProductRecipeParseResult.Success(lines);
    }

    private sealed record ProductRecipeLine(string MaterialId, double Quantity);

    private sealed class ProductRecipeParseResult
    {
        public bool IsValid { get; init; }
        public string Message { get; init; } = string.Empty;
        public IReadOnlyList<ProductRecipeLine> Lines { get; init; } = Array.Empty<ProductRecipeLine>();

        public static ProductRecipeParseResult Error(string message) => new() { IsValid = false, Message = message };
        public static ProductRecipeParseResult Success(IReadOnlyList<ProductRecipeLine> lines) => new() { IsValid = true, Lines = lines };
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteModal([FromForm] string id)
    {
        id = (id ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { message = "كود غير صحيح." });
        }

        var product = await _context.Products.FindAsync(id).ConfigureAwait(false);
        if (product is null)
        {
            return Json(new { ok = true });
        }

        var usedInOrders = await _context.OrderDetails.AnyAsync(od => od.ProductId == id).ConfigureAwait(false);
        if (usedInOrders)
        {
            return Conflict(new { message = "لا يمكن حذف المنتج لأنه مستخدم في طلبات." });
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Json(new { ok = true });
    }
}

