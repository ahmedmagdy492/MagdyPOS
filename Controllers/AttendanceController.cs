using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MagdyPOS.Controllers;

[Authorize]
public sealed class AttendanceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> My()
    {
        ViewData["Title"] = "الحضور والانصراف";
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var today = DateTime.Today;
        var record = await _context.AttendanceRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == userId && r.WorkDate == today)
            .ConfigureAwait(false);

        return View(new MyAttendanceViewModel
        {
            WorkDate = today,
            TodayRecord = record,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var today = DateTime.Today;
        var exists = await _context.AttendanceRecords
            .AnyAsync(r => r.UserId == userId && r.WorkDate == today)
            .ConfigureAwait(false);

        if (exists)
        {
            TempData["AttendanceError"] = "تم تسجيل الحضور بالفعل لهذا اليوم.";
            return RedirectToAction(nameof(My));
        }

        _context.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = userId,
            WorkDate = today,
            CheckInAtUtc = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync().ConfigureAwait(false);
        TempData["AttendanceMessage"] = "تم تسجيل الحضور.";
        return RedirectToAction(nameof(My));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var today = DateTime.Today;
        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.UserId == userId && r.WorkDate == today)
            .ConfigureAwait(false);

        if (record is null)
        {
            TempData["AttendanceError"] = "لا يوجد حضور مسجل لهذا اليوم.";
            return RedirectToAction(nameof(My));
        }

        if (record.CheckOutAtUtc.HasValue)
        {
            TempData["AttendanceError"] = "تم تسجيل الانصراف بالفعل لهذا اليوم.";
            return RedirectToAction(nameof(My));
        }

        record.CheckOutAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync().ConfigureAwait(false);
        TempData["AttendanceMessage"] = "تم تسجيل الانصراف.";
        return RedirectToAction(nameof(My));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    public async Task<IActionResult> Week([FromQuery] DateTime? date = null)
    {
        ViewData["Title"] = "حضور الموظفين - الأسبوع الحالي";

        var target = (date ?? DateTime.Today).Date;
        // Week starts on Saturday (common in Egypt), ends next Saturday.
        var diff = ((int)target.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        var weekStart = target.AddDays(-diff).Date;
        var weekEnd = weekStart.AddDays(7).Date;

        var days = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var d = weekStart.AddDays(i).Date;
                return new AttendanceWeekDayColumnViewModel
                {
                    Date = d,
                    Label = d.ToString("ddd dd", new System.Globalization.CultureInfo("ar-EG")),
                };
            })
            .ToList();

        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.Email)
            .ToListAsync()
            .ConfigureAwait(false);

        var userIds = users.Select(u => u.Id).ToList();

        var records = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(r => userIds.Contains(r.UserId))
            .Where(r => r.WorkDate >= weekStart && r.WorkDate < weekEnd)
            .ToListAsync()
            .ConfigureAwait(false);

        var recLookup = records
            .GroupBy(r => r.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyDictionary<DateTime, AttendanceRecord>)g.ToDictionary(x => x.WorkDate.Date, x => x));

        var rows = users.Select(u =>
        {
            recLookup.TryGetValue(u.Id, out var map);
            map ??= new Dictionary<DateTime, AttendanceRecord>();
            return new AttendanceWeekUserRowViewModel
            {
                UserId = u.Id,
                UserName = u.Email ?? u.UserName ?? "—",
                FullName = u.FullName,
                RecordsByDate = map,
            };
        }).ToList();

        return View(new AttendanceWeekReportViewModel
        {
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            Days = days,
            Users = rows,
        });
    }
}

