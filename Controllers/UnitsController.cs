using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagdyPOS.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UnitsController : Controller
{
    private readonly ApplicationDbContext _context;

    public UnitsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "الوحدات";
        var units = await _context.Units.OrderBy(u => u.Name).ToListAsync().ConfigureAwait(false);
        return View(units);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateModal([FromForm] string name, [FromForm] string symbol, [FromForm] string? notes)
    {
        name = (name ?? string.Empty).Trim();
        symbol = (symbol ?? string.Empty).Trim();
        notes = notes?.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest(new { message = "الاسم والرمز مطلوبان." });
        }

        var unit = new Unit
        {
            Name = name,
            Symbol = symbol,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes,
        };

        _context.Units.Add(unit);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        return Json(new { id = unit.Id, name = unit.Name, symbol = unit.Symbol, notes = unit.Notes ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditModal([FromForm] long id, [FromForm] string name, [FromForm] string symbol, [FromForm] string? notes)
    {
        name = (name ?? string.Empty).Trim();
        symbol = (symbol ?? string.Empty).Trim();
        notes = notes?.Trim();

        if (id <= 0)
        {
            return BadRequest(new { message = "معرّف غير صالح." });
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(symbol))
        {
            return BadRequest(new { message = "الاسم والرمز مطلوبان." });
        }

        var unit = await _context.Units.FindAsync(id).ConfigureAwait(false);
        if (unit is null)
        {
            return NotFound(new { message = "الوحدة غير موجودة." });
        }

        unit.Name = name;
        unit.Symbol = symbol;
        unit.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes;

        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Json(new { id = unit.Id, name = unit.Name, symbol = unit.Symbol, notes = unit.Notes ?? string.Empty });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteModal([FromForm] long id)
    {
        if (id <= 0)
        {
            return BadRequest(new { message = "معرّف غير صالح." });
        }

        var unit = await _context.Units.FindAsync(id).ConfigureAwait(false);
        if (unit is null)
        {
            return Json(new { ok = true });
        }

        var isUsed = await _context.Materials.AnyAsync(m => m.UnitId == id).ConfigureAwait(false)
            || await _context.Products.AnyAsync(p => p.UnitId == id).ConfigureAwait(false);

        if (isUsed)
        {
            return Conflict(new { message = "لا يمكن حذف الوحدة لأنها مستخدمة في الخامات أو المنتجات." });
        }

        _context.Units.Remove(unit);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Json(new { ok = true });
    }

    public IActionResult Create()
    {
        ViewData["Title"] = "إضافة وحدة";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Name,Symbol,Notes")] Unit unit)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "إضافة وحدة";
            return View(unit);
        }

        _context.Units.Add(unit);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var unit = await _context.Units.FindAsync(id.Value).ConfigureAwait(false);
        if (unit is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "تعديل وحدة";
        return View(unit);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, [Bind("Id,Name,Symbol,Notes")] Unit unit)
    {
        if (id != unit.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "تعديل وحدة";
            return View(unit);
        }

        try
        {
            _context.Update(unit);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Units.AnyAsync(u => u.Id == unit.Id).ConfigureAwait(false))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(long? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == id.Value).ConfigureAwait(false);
        if (unit is null)
        {
            return NotFound();
        }

        ViewData["Title"] = "حذف وحدة";
        return View(unit);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(long id)
    {
        var unit = await _context.Units.FindAsync(id).ConfigureAwait(false);
        if (unit is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var isUsed = await _context.Materials.AnyAsync(m => m.UnitId == id).ConfigureAwait(false)
            || await _context.Products.AnyAsync(p => p.UnitId == id).ConfigureAwait(false);

        if (isUsed)
        {
            TempData["Error"] = "لا يمكن حذف الوحدة لأنها مستخدمة في الخامات أو المنتجات.";
            return RedirectToAction(nameof(Index));
        }

        _context.Units.Remove(unit);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return RedirectToAction(nameof(Index));
    }
}
