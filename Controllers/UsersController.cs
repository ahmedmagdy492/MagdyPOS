using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagdyPOS.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ApplicationDbContext _db;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "المستخدمون";
        var rows = new List<UserListRowVm>();
        foreach (var u in await _userManager.Users.AsNoTracking().OrderBy(x => x.Email).ToListAsync().ConfigureAwait(false))
        {
            var roles = await _userManager.GetRolesAsync(u).ConfigureAwait(false);
            rows.Add(new UserListRowVm
            {
                Id = u.Id,
                UserName = u.UserName ?? "",
                Email = u.Email,
                FullName = u.FullName,
                Roles = string.Join("، ", roles),
            });
        }

        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserInput model)
    {
        if (!ModelState.IsValid)
        {
            TempData["UsersError"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        if (!ValidRole(model.Role))
        {
            TempData["UsersError"] = "صلاحية غير صالحة.";
            return RedirectToAction(nameof(Index));
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            EmailConfirmed = true,
            FullName = string.IsNullOrWhiteSpace(model.FullName) ? null : model.FullName.Trim(),
        };

        var result = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            TempData["UsersError"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        await _userManager.AddToRoleAsync(user, model.Role).ConfigureAwait(false);
        TempData["UsersMessage"] = "تم إنشاء المستخدم.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserInput model)
    {
        if (!ModelState.IsValid)
        {
            TempData["UsersError"] = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        if (!ValidRole(model.Role))
        {
            TempData["UsersError"] = "صلاحية غير صالحة.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(model.Id).ConfigureAwait(false);
        if (user is null)
        {
            TempData["UsersError"] = "المستخدم غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        user.Email = model.Email.Trim();
        user.UserName = model.Email.Trim();
        user.FullName = string.IsNullOrWhiteSpace(model.FullName) ? null : model.FullName.Trim();

        var updateResult = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!updateResult.Succeeded)
        {
            TempData["UsersError"] = string.Join(" ", updateResult.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            var pwdResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword).ConfigureAwait(false);
            if (!pwdResult.Succeeded)
            {
                TempData["UsersError"] = string.Join(" ", pwdResult.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Index));
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        await _userManager.RemoveFromRolesAsync(user, currentRoles).ConfigureAwait(false);
        await _userManager.AddToRoleAsync(user, model.Role).ConfigureAwait(false);

        TempData["UsersMessage"] = "تم تحديث المستخدم.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            TempData["UsersError"] = "معرّف غير صالح.";
            return RedirectToAction(nameof(Index));
        }

        var current = await _userManager.GetUserAsync(User).ConfigureAwait(false);
        if (current is not null && current.Id == id)
        {
            TempData["UsersError"] = "لا يمكنك حذف حسابك الحالي.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.FindByIdAsync(id).ConfigureAwait(false);
        if (user is null)
        {
            TempData["UsersError"] = "المستخدم غير موجود.";
            return RedirectToAction(nameof(Index));
        }

        if (await _userManager.IsInRoleAsync(user, AppRoles.Admin).ConfigureAwait(false))
        {
            var adminCount = await CountUsersInRoleAsync(AppRoles.Admin).ConfigureAwait(false);
            if (adminCount <= 1)
            {
                TempData["UsersError"] = "يجب أن يبقى مدير واحد على الأقل في النظام.";
                return RedirectToAction(nameof(Index));
            }
        }

        var result = await _userManager.DeleteAsync(user).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            TempData["UsersError"] = string.Join(" ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }

        TempData["UsersMessage"] = "تم حذف المستخدم.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<int> CountUsersInRoleAsync(string roleName)
    {
        var role = await _roleManager.FindByNameAsync(roleName).ConfigureAwait(false);
        if (role is null)
        {
            return 0;
        }

        return await _db.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == role.Id).ConfigureAwait(false);
    }

    private static bool ValidRole(string role) =>
        string.Equals(role, AppRoles.Admin, StringComparison.Ordinal)
        || string.Equals(role, AppRoles.Sales, StringComparison.Ordinal);
}
