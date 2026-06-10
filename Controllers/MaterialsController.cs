using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MagdyPOS.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class MaterialsController : Controller
{
    private readonly ApplicationDbContext _context;

    public MaterialsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] int pageNo = 1, [FromQuery] int pageSize = 20)
    {
        ViewData["Title"] = "الخامات";
        if (pageNo < 1) pageNo = 1;
        if (pageSize < 1) pageSize = 20;

        var query = _context.Materials
            .AsNoTracking()
            .Include(m => m.Unit)
            .OrderBy(m => m.Name);

        ViewBag.TotalCount = await query.CountAsync().ConfigureAwait(false);

        var materials = await query
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync()
            .ConfigureAwait(false);
        ViewBag.Units = await _context.Units.OrderBy(u => u.Name).ToListAsync().ConfigureAwait(false);
        return View(materials);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CreateModal([FromForm] string id, [FromForm] string name, [FromForm] double quantity, [FromForm] long unitId, [FromForm] double alertLimit)
    {
        _ = id;
        _ = name;
        _ = quantity;
        _ = unitId;
        _ = alertLimit;
        return BadRequest(new { message = "تم إيقاف إضافة الخامات من هذه الصفحة. أضف الخامة من صفحة المصروفات." });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditModal([FromForm] string id, [FromForm] string name, [FromForm] double quantity, [FromForm] long unitId, [FromForm] double alertLimit)
    {
        id = (id ?? string.Empty).Trim();
        name = (name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { message = "الكود غير صحيح." });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "الاسم مطلوب." });
        }
        if (quantity < 0)
        {
            return BadRequest(new { message = "الكميات لا يمكن أن تكون سالبة." });
        }

        var material = await _context.Materials.FindAsync(id).ConfigureAwait(false);
        if (material is null)
        {
            return NotFound(new { message = "الخامة غير موجودة." });
        }

        var beforeQty = material.Quantity;

        var unit = await _context.Units.FindAsync(unitId).ConfigureAwait(false);
        if (unit is null)
        {
            return BadRequest(new { message = "الوحدة غير صحيحة." });
        }

        material.Name = name;
        material.Quantity = quantity;
        material.OriginalQuantity = quantity;
        material.UnitId = unitId;
        material.AlertLimit = alertLimit;

        var delta = quantity - beforeQty;
        if (Math.Abs(delta) > 0.0000001)
        {
            var kind = delta > 0 ? StockMovementKind.In : StockMovementKind.Out;
            _context.StockMovements.Add(new StockMovement
            {
                ItemType = StockItemType.Material,
                ItemId = material.Id,
                ItemName = material.Name,
                Kind = kind,
                QuantityDelta = delta,
                QuantityBefore = beforeQty,
                QuantityAfter = quantity,
                Reason = "تعديل كمية الخامة يدوياً",
                ReferenceType = "Materials/EditModal",
                ReferenceId = material.Id,
                UserId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier),
                UserName = User.Identity?.Name,
                CreatedAt = DateTime.UtcNow,
            });
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);

        return Json(new
        {
            id = material.Id,
            name = material.Name,
            quantity = material.Quantity,
            originalQuantity = material.OriginalQuantity,
            createdAt = material.CreatedAt,
            alertLimit = material.AlertLimit,
            unitId = unit.Id,
            unitName = unit.Name,
            unitSymbol = unit.Symbol,
        });
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

        var material = await _context.Materials.FindAsync(id).ConfigureAwait(false);
        if (material is null)
        {
            return Json(new { ok = true });
        }

        var isUsed = await _context.ProductMaterials.AnyAsync(pm => pm.MaterialId == id).ConfigureAwait(false);
        if (isUsed)
        {
            return Conflict(new { message = "لا يمكن حذف الخامة لأنها مرتبطة بمنتج واحد أو أكثر." });
        }

        _context.Materials.Remove(material);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Json(new { ok = true });
    }

    public IActionResult Create()
    {
        TempData["Error"] = "إضافة الخامات متاحة فقط من صفحة المصروفات.";
        return RedirectToAction("Create", "Expenses");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("Id,Name,Quantity,OriginalQuantity,CreatedAt,UnitId,AlertLimit")] Material material)
    {
        _ = material;
        TempData["Error"] = "إضافة الخامات متاحة فقط من صفحة المصروفات.";
        return RedirectToAction("Create", "Expenses");
    }

    public async Task<IActionResult> Edit(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var material = await _context.Materials.FindAsync(id).ConfigureAwait(false);
        if (material is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "تعديل خامة";
        PopulateUnits(material.UnitId);
        return View(material);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, [Bind("Id,Name,Quantity,OriginalQuantity,CreatedAt,UnitId,AlertLimit")] Material material)
    {
        if (!string.Equals(id, material.Id, StringComparison.Ordinal))
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "تعديل خامة";
            PopulateUnits(material.UnitId);
            return View(material);
        }

        try
        {
            _context.Update(material);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Materials.AnyAsync(m => m.Id == material.Id).ConfigureAwait(false))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var material = await _context.Materials
            .Include(m => m.Unit)
            .FirstOrDefaultAsync(m => m.Id == id)
            .ConfigureAwait(false);
        if (material is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "حذف خامة";
        return View(material);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var material = await _context.Materials.FindAsync(id).ConfigureAwait(false);
        if (material is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var isUsed = await _context.ProductMaterials.AnyAsync(pm => pm.MaterialId == id).ConfigureAwait(false);
        if (isUsed)
        {
            TempData["Error"] = "لا يمكن حذف الخامة لأنها مرتبطة بمنتج واحد أو أكثر.";
            return RedirectToAction(nameof(Index));
        }

        _context.Materials.Remove(material);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return RedirectToAction(nameof(Index));
    }

    private void PopulateUnits(long? selectedUnit = null)
    {
        ViewBag.UnitId = new SelectList(_context.Units.OrderBy(u => u.Name), "Id", "Name", selectedUnit);
    }
}
