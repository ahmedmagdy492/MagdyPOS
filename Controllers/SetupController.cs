using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagdyPOS.Controllers;

public class SetupController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;

    public SetupController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> FirstAdmin()
    {
        if (await HasAdminAsync().ConfigureAwait(false))
        {
            return RedirectToAction("Login", "Login");
        }

        ViewData["Title"] = "تهيئة النظام";
        return View(new FirstAdminRegistrationViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FirstAdmin(FirstAdminRegistrationViewModel model)
    {
        if (await HasAdminAsync().ConfigureAwait(false))
        {
            return RedirectToAction("Login", "Login");
        }

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = "تهيئة النظام";
            return View(model);
        }

        var email = model.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (existingUser is not null)
        {
            ModelState.AddModelError(nameof(model.Email), "هذا البريد الإلكتروني مستخدم بالفعل.");
            ViewData["Title"] = "تهيئة النظام";
            return View(model);
        }

        var adminUser = new ApplicationUser
        {
            FullName = model.FullName.Trim(),
            Email = email,
            UserName = email,
            EmailConfirmed = true
        };

        var createResult = await _userManager.CreateAsync(adminUser, model.Password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["Title"] = "تهيئة النظام";
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(adminUser, AppRoles.Admin).ConfigureAwait(false);
        if (!roleResult.Succeeded)
        {
            foreach (var error in roleResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            await _userManager.DeleteAsync(adminUser).ConfigureAwait(false);
            ViewData["Title"] = "تهيئة النظام";
            return View(model);
        }

        _context.OrganizationProfiles.Add(new OrganizationProfile
        {
            Name = model.OrganizationName.Trim(),
            Phone = model.OrganizationPhone.Trim(),
            Address = model.OrganizationAddress.Trim()
        });
        await _context.SaveChangesAsync().ConfigureAwait(false);

        await _signInManager.SignInAsync(adminUser, isPersistent: false).ConfigureAwait(false);
        return RedirectToAction("Index", "Admin");
    }

    private async Task<bool> HasAdminAsync()
    {
        var adminRoleId = await _context.Roles
            .AsNoTracking()
            .Where(r => r.Name == AppRoles.Admin)
            .Select(r => r.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(adminRoleId))
        {
            return false;
        }

        return await _context.UserRoles
            .AsNoTracking()
            .AnyAsync(ur => ur.RoleId == adminRoleId)
            .ConfigureAwait(false);
    }
}
