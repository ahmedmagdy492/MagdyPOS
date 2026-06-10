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
public sealed class ExpensesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ExpensesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string q, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 20)
    {
        ViewData["Title"] = "المصروفات";

        if (pageNo < 1) pageNo = 1;
        if (pageSize < 1) pageSize = 20;
        q = (q ?? string.Empty).Trim();

        if (from is null) from = DateTime.Now.AddDays(-7).Date;
        if (to is null) to = DateTime.Now.Date.AddDays(1).AddTicks(-1);
        if (from > to)
        {
            ViewBag.DateFilterError = "تاريخ البداية بعد تاريخ النهاية";
            return View(new List<Expense>());
        }

        var query = _context.Expenses
            .AsNoTracking()
            .Include(e => e.Material)
            .Where(e => e.CreatedAt >= from && e.CreatedAt <= to)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(e =>
                e.Title.Contains(q) ||
                (e.Notes != null && e.Notes.Contains(q)) ||
                (e.Material != null && (e.Material.Name.Contains(q) || e.Material.Id.Contains(q))));
        }

        ViewBag.TotalCount = await query.CountAsync().ConfigureAwait(false);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Skip((pageNo - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync()
            .ConfigureAwait(false);

        ViewBag.From = from.Value.ToString("yyyy-MM-dd");
        ViewBag.To = to.Value.ToString("yyyy-MM-dd");
        ViewBag.Query = q;

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewData["Title"] = "إضافة مصروف";
        await PopulateMaterialsAndUnitsAsync().ConfigureAwait(false);
        return View(new Expense { CreatedAt = DateTime.Now });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Amount,Notes,CreatedAt,MaterialId,MaterialQuantity")] Expense expense)
    {
        NormalizeExpense(expense);
        ValidateExpense(expense);
        var resolvedMaterialId = await ResolveMaterialSelectionAsync(expense).ConfigureAwait(false);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "إضافة مصروف";
            await PopulateMaterialsAndUnitsAsync(expense.MaterialId).ConfigureAwait(false);
            return View(expense);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        expense.UserId = userId;
        expense.UserName = User.Identity?.Name;

        await using var trx = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            await ApplyMaterialEffectAsync(
                oldMaterialId: null,
                oldQty: null,
                newMaterialId: resolvedMaterialId,
                newQty: expense.MaterialQuantity,
                referenceType: "Expense",
                referenceId: expense.Id.ToString()).ConfigureAwait(false);

            await _context.SaveChangesAsync().ConfigureAwait(false);

            await trx.CommitAsync().ConfigureAwait(false);
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            await trx.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var expense = await _context.Expenses
            .AsNoTracking()
            .Include(e => e.Material)
            .FirstOrDefaultAsync(e => e.Id == id)
            .ConfigureAwait(false);

        if (expense is null)
        {
            return NotFound();
        }

        ViewData["Title"] = $"تفاصيل مصروف #{expense.Id}";
        return View(expense);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        var expense = await _context.Expenses.FindAsync(id).ConfigureAwait(false);
        if (expense is null) return NotFound();

        ViewData["Title"] = "تعديل مصروف";
        await PopulateMaterialsAndUnitsAsync(expense.MaterialId).ConfigureAwait(false);
        return View(expense);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [Bind("Id,Title,Amount,Notes,CreatedAt,MaterialId,MaterialQuantity")] Expense form)
    {
        if (id != form.Id) return NotFound();

        NormalizeExpense(form);
        ValidateExpense(form);
        var resolvedMaterialId = await ResolveMaterialSelectionAsync(form).ConfigureAwait(false);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "تعديل مصروف";
            await PopulateMaterialsAndUnitsAsync(form.MaterialId).ConfigureAwait(false);
            return View(form);
        }

        var existing = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id).ConfigureAwait(false);
        if (existing is null) return NotFound();

        var oldMaterialId = existing.MaterialId;
        var oldQty = existing.MaterialQuantity;

        await using var trx = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            await ApplyMaterialEffectAsync(oldMaterialId, oldQty, resolvedMaterialId, form.MaterialQuantity, referenceType: "Expenses/Edit", referenceId: id.ToString()).ConfigureAwait(false);

            existing.Title = form.Title;
            existing.Amount = form.Amount;
            existing.Notes = form.Notes;
            existing.CreatedAt = form.CreatedAt;
            existing.MaterialId = resolvedMaterialId;
            existing.MaterialQuantity = form.MaterialQuantity;
            existing.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            existing.UserName = User.Identity?.Name;

            await _context.SaveChangesAsync().ConfigureAwait(false);
            await trx.CommitAsync().ConfigureAwait(false);

            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            await trx.RollbackAsync().ConfigureAwait(false);
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewData["Title"] = "تعديل مصروف";
            await PopulateMaterialsAndUnitsAsync(form.MaterialId).ConfigureAwait(false);
            return View(form);
        }
        catch
        {
            await trx.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(long id)
    {
        var expense = await _context.Expenses
            .AsNoTracking()
            .Include(e => e.Material)
            .FirstOrDefaultAsync(e => e.Id == id)
            .ConfigureAwait(false);

        if (expense is null) return NotFound();

        ViewData["Title"] = "حذف مصروف";
        return View(expense);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id).ConfigureAwait(false);
        if (expense is null) return RedirectToAction(nameof(Index));

        await using var trx = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
        try
        {
            await ApplyMaterialEffectAsync(expense.MaterialId, expense.MaterialQuantity, newMaterialId: null, newQty: null, referenceType: "Expenses/Delete", referenceId: id.ToString()).ConfigureAwait(false);

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            await trx.CommitAsync().ConfigureAwait(false);
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            await trx.RollbackAsync().ConfigureAwait(false);
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Delete), new { id });
        }
        catch
        {
            await trx.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task PopulateMaterialsAndUnitsAsync(string? selectedMaterialId = null, long? selectedUnitId = null)
    {
        var materials = await _context.Materials
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new { m.Id, Label = $"{m.Name} ({m.Id})" })
            .ToListAsync()
            .ConfigureAwait(false);

        ViewBag.MaterialId = new SelectList(materials, "Id", "Label", selectedMaterialId);

        var units = await _context.Units
            .AsNoTracking()
            .OrderBy(u => u.Name)
            .ToListAsync()
            .ConfigureAwait(false);
        ViewBag.MaterialUnits = new SelectList(units, "Id", "Name", selectedUnitId);
    }

    private static void NormalizeExpense(Expense expense)
    {
        expense.Title = (expense.Title ?? string.Empty).Trim();
        expense.Notes = string.IsNullOrWhiteSpace(expense.Notes) ? null : expense.Notes.Trim();
        expense.MaterialId = string.IsNullOrWhiteSpace(expense.MaterialId) ? null : expense.MaterialId.Trim();
        if (!expense.MaterialQuantity.HasValue) return;
        if (Math.Abs(expense.MaterialQuantity.Value) < 0.0000001) expense.MaterialQuantity = null;
    }

    private void ValidateExpense(Expense expense)
    {
        var createNewMaterial = string.Equals(Request.Form["CreateNewMaterial"], "true", StringComparison.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(expense.Title))
        {
            ModelState.AddModelError(nameof(Expense.Title), "العنوان مطلوب.");
        }
        if (expense.Amount <= 0)
        {
            ModelState.AddModelError(nameof(Expense.Amount), "قيمة المصروف يجب أن تكون أكبر من صفر.");
        }

        if (createNewMaterial)
        {
            if (!expense.MaterialQuantity.HasValue || expense.MaterialQuantity.Value <= 0)
            {
                ModelState.AddModelError(nameof(Expense.MaterialQuantity), "كمية الخامة مطلوبة ويجب أن تكون أكبر من صفر عند إنشاء خامة جديدة.");
            }
            return;
        }

        if (!string.IsNullOrWhiteSpace(expense.MaterialId))
        {
            if (!expense.MaterialQuantity.HasValue || expense.MaterialQuantity.Value <= 0)
            {
                ModelState.AddModelError(nameof(Expense.MaterialQuantity), "كمية الخامة مطلوبة ويجب أن تكون أكبر من صفر عند اختيار خامة.");
            }
        }
        else
        {
            expense.MaterialQuantity = null;
        }
    }

    private static bool HasMaterialEffect(string? materialId, double? qty)
        => !string.IsNullOrWhiteSpace(materialId) && qty.HasValue && qty.Value > 0;

    private async Task<string?> ResolveMaterialSelectionAsync(Expense expense)
    {
        var createNewMaterial = string.Equals(Request.Form["CreateNewMaterial"], "true", StringComparison.OrdinalIgnoreCase);
        if (!createNewMaterial)
        {
            return expense.MaterialId;
        }

        var newMaterialId = (Request.Form["NewMaterialId"].ToString() ?? string.Empty).Trim();
        var newMaterialName = (Request.Form["NewMaterialName"].ToString() ?? string.Empty).Trim();
        var newMaterialUnitRaw = Request.Form["NewMaterialUnitId"].ToString();
        var newMaterialAlertRaw = Request.Form["NewMaterialAlertLimit"].ToString();

        if (string.IsNullOrWhiteSpace(newMaterialId))
        {
            ModelState.AddModelError("NewMaterialId", "كود الخامة الجديدة مطلوب.");
            return expense.MaterialId;
        }

        if (string.IsNullOrWhiteSpace(newMaterialName))
        {
            ModelState.AddModelError("NewMaterialName", "اسم الخامة الجديدة مطلوب.");
            return expense.MaterialId;
        }

        if (!long.TryParse(newMaterialUnitRaw, out var newMaterialUnitId) || newMaterialUnitId <= 0)
        {
            ModelState.AddModelError("NewMaterialUnitId", "اختر وحدة صحيحة للخامة الجديدة.");
            return expense.MaterialId;
        }

        if (!double.TryParse(newMaterialAlertRaw, out var newMaterialAlert))
        {
            newMaterialAlert = 0;
        }

        if (!expense.MaterialQuantity.HasValue || expense.MaterialQuantity.Value <= 0)
        {
            ModelState.AddModelError(nameof(Expense.MaterialQuantity), "كمية الخامة يجب أن تكون أكبر من صفر عند إنشاء خامة جديدة.");
            return expense.MaterialId;
        }

        var unitExists = await _context.Units.AsNoTracking().AnyAsync(u => u.Id == newMaterialUnitId).ConfigureAwait(false);
        if (!unitExists)
        {
            ModelState.AddModelError("NewMaterialUnitId", "الوحدة المختارة غير موجودة.");
            return expense.MaterialId;
        }

        var existing = await _context.Materials.AsNoTracking().AnyAsync(m => m.Id == newMaterialId).ConfigureAwait(false);
        if (existing)
        {
            ModelState.AddModelError("NewMaterialId", "كود الخامة الجديدة مستخدم بالفعل.");
            return expense.MaterialId;
        }

        _context.Materials.Add(new Material
        {
            Id = newMaterialId,
            Name = newMaterialName,
            UnitId = newMaterialUnitId,
            AlertLimit = newMaterialAlert,
            Quantity = 0,
            OriginalQuantity = 0,
            CreatedAt = expense.CreatedAt
        });

        expense.MaterialId = newMaterialId;
        return newMaterialId;
    }

    private async Task ApplyMaterialEffectAsync(string? oldMaterialId, double? oldQty, string? newMaterialId, double? newQty, string referenceType, string? referenceId)
    {
        // Revert old effect
        if (HasMaterialEffect(oldMaterialId, oldQty))
        {
            var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == oldMaterialId).ConfigureAwait(false);
            if (material is null)
            {
                throw new InvalidOperationException("Material not found for old expense link.");
            }

            var before = material.Quantity;
            var after = before - oldQty!.Value;
            if (after < 0)
            {
                throw new InvalidOperationException("لا يمكن تنفيذ العملية لأن تأثير المصروف سيجعل المخزون سالباً.");
            }

            material.Quantity = after;

            _context.StockMovements.Add(new StockMovement
            {
                ItemType = StockItemType.Material,
                ItemId = material.Id,
                ItemName = material.Name,
                Kind = StockMovementKind.Out,
                QuantityDelta = -oldQty.Value,
                QuantityBefore = before,
                QuantityAfter = after,
                Reason = "عكس تأثير مصروف مرتبط بخامة",
                ReferenceType = referenceType,
                ReferenceId = referenceId ?? string.Empty,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = User.Identity?.Name,
                CreatedAt = DateTime.UtcNow,
            });
        }

        // Apply new effect
        if (HasMaterialEffect(newMaterialId, newQty))
        {
            var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == newMaterialId).ConfigureAwait(false);
            if (material is null)
            {
                ModelState.AddModelError(nameof(Expense.MaterialId), "الخامة غير موجودة.");
                throw new InvalidOperationException("Material not found for new expense link.");
            }

            var before = material.Quantity;
            var after = before + newQty!.Value;
            material.Quantity = after;

            _context.StockMovements.Add(new StockMovement
            {
                ItemType = StockItemType.Material,
                ItemId = material.Id,
                ItemName = material.Name,
                Kind = StockMovementKind.In,
                QuantityDelta = newQty.Value,
                QuantityBefore = before,
                QuantityAfter = after,
                Reason = "تأثير مصروف شراء خامة على المخزون",
                ReferenceType = referenceType,
                ReferenceId = referenceId ?? string.Empty,
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = User.Identity?.Name,
                CreatedAt = DateTime.UtcNow,
            });
        }
    }

    
}

