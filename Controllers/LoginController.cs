using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagdyPOS.Controllers;

public class LoginController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public LoginController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _db = db;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string returnUrl)
    {
        var hasAdmin = _db.Roles
            .Where(r => r.Name == AppRoles.Admin)
            .Join(_db.UserRoles, role => role.Id, userRole => userRole.RoleId, (_, _) => 1)
            .Any();

        if (!hasAdmin)
        {
            return RedirectToAction("FirstAdmin", "Setup");
        }

        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole(AppRoles.Admin))
            {
                return RedirectToAction("Index", "Admin");
            }

            if (User.IsInRole(AppRoles.Sales))
            {
                return RedirectToAction("Index", "Pos");
            }

            return RedirectToAction("Index", "Home");
        }

        ViewData["Title"] = "تسجيل الدخول";
        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "تسجيل الدخول";
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Username.Trim()).ConfigureAwait(false);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            ViewData["Title"] = "تسجيل الدخول";
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!,
            model.Password,
            isPersistent: false,
            lockoutOnFailure: false).ConfigureAwait(false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            ViewData["Title"] = "تسجيل الدخول";
            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        // Attendance "fingerprint" behavior:
        // Successful login = check-in (once per day).
        await EnsureCheckedInTodayAsync(user.Id).ConfigureAwait(false);

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin).ConfigureAwait(false))
        {
            return RedirectToAction("Index", "Admin");
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.Sales).ConfigureAwait(false))
        {
            return RedirectToAction("Index", "Pos");
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // Attendance "fingerprint" behavior:
        // Logout = check-out (if checked-in today and not checked-out yet).
        var current = await _userManager.GetUserAsync(User).ConfigureAwait(false);
        if (current is not null)
        {
            await EnsureCheckedOutTodayAsync(current.Id).ConfigureAwait(false);
        }

        await _signInManager.SignOutAsync().ConfigureAwait(false);
        return RedirectToAction(nameof(Login));
    }

    private async Task EnsureCheckedInTodayAsync(string userId)
    {
        var today = DateTime.Today;
        var exists = await _db.AttendanceRecords
            .AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.WorkDate == today)
            .ConfigureAwait(false);

        if (exists) return;

        _db.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = userId,
            WorkDate = today,
            CheckInAtUtc = DateTime.UtcNow,
        });
        await _db.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task EnsureCheckedOutTodayAsync(string userId)
    {
        var today = DateTime.Today;
        var record = await _db.AttendanceRecords
            .FirstOrDefaultAsync(r => r.UserId == userId && r.WorkDate == today)
            .ConfigureAwait(false);

        if (record is null) return;
        if (record.CheckOutAtUtc.HasValue) return;

        record.CheckOutAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync().ConfigureAwait(false);
    }
}
