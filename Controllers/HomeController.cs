using System.Diagnostics;
using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MagdyPOS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index()
        {
            var hasAdmin = _context.Roles
                .Where(r => r.Name == AppRoles.Admin)
                .Join(_context.UserRoles, role => role.Id, userRole => userRole.RoleId, (_, _) => 1)
                .Any();

            if (!hasAdmin)
            {
                return RedirectToAction("FirstAdmin", "Setup");
            }

            // Public marketing landing page for anonymous visitors.
            // Logged-in users go straight to their workspace.
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
            }

            ViewData["Title"] = "MagdyPOS - برنامج كاشير للمطاعم والكافيهات (بدون نت)";
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
