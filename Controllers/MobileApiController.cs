using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MagdyPOS.Controllers;

[ApiController]
[Route("api/mobile")]
public sealed class MobileApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;

    public MobileApiController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IConfiguration configuration)
    {
        _context = context;
        _userManager = userManager;
        _configuration = configuration;
    }

    [HttpPost("auth/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] MobileLoginRequest request)
    {
        var email = (request.Email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required." });
        }

        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var valid = await _userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
        if (!valid)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var token = BuildToken(user, roles);
        await EnsureCheckedInTodayAsync(user.Id).ConfigureAwait(false);

        return Ok(new
        {
            token,
            user = new
            {
                id = user.Id,
                fullName = user.FullName ?? user.Email ?? user.UserName ?? "User",
                email = user.Email,
                role = roles.Contains(AppRoles.Admin) ? AppRoles.Admin : AppRoles.Sales,
            }
        });
    }

    [HttpGet("dashboard")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Dashboard()
    {
        var start = DateTime.Today;
        var end = start.AddDays(1);
        var todayOrders = await _context.Orders.AsNoTracking().Where(o => o.OrderDate >= start && o.OrderDate < end).ToListAsync().ConfigureAwait(false);
        var gross = await _context.OrderDetails.AsNoTracking()
            .Where(d => d.Order.OrderDate >= start && d.Order.OrderDate < end)
            .SumAsync(d => (double?)(d.UnitPrice * d.Quantity)).ConfigureAwait(false) ?? 0;
        var discount = todayOrders.Sum(o => o.Discount);
        var lowStock = await _context.Materials.AsNoTracking().CountAsync(m => m.Quantity <= m.AlertLimit).ConfigureAwait(false);
        return Ok(new
        {
            todaySales = Math.Max(0, gross - discount),
            todayOrders = todayOrders.Count,
            materialsCount = await _context.Materials.AsNoTracking().CountAsync().ConfigureAwait(false),
            lowStockCount = lowStock,
        });
    }

    [HttpGet("products")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Products()
    {
        var items = await _context.Products.AsNoTracking()
            .Include(p => p.Unit)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Price, p.UnitId, unitName = p.Unit.Name, unitSymbol = p.Unit.Symbol })
            .ToListAsync()
            .ConfigureAwait(false);
        return Ok(items);
    }

    [HttpPost("products")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateProduct([FromBody] MobileProductWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Id and name are required." });
        }

        var exists = await _context.Products.AnyAsync(p => p.Id == request.Id).ConfigureAwait(false);
        if (exists) return Conflict(new { message = "Product already exists." });

        _context.Products.Add(new Product
        {
            Id = request.Id.Trim(),
            Name = request.Name.Trim(),
            Price = request.Price,
            UnitId = request.UnitId,
        });
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Ok(new { ok = true });
    }

    [HttpGet("units")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Units()
    {
        var units = await _context.Units.AsNoTracking().OrderBy(x => x.Name).ToListAsync().ConfigureAwait(false);
        return Ok(units.Select(u => new { u.Id, u.Name, u.Symbol, u.Notes }));
    }

    [HttpPost("units")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateUnit([FromBody] MobileUnitWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Symbol))
        {
            return BadRequest(new { message = "Name and symbol are required." });
        }
        _context.Units.Add(new Unit { Name = request.Name.Trim(), Symbol = request.Symbol.Trim(), Notes = request.Notes?.Trim() });
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Ok(new { ok = true });
    }

    [HttpGet("materials")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Materials()
    {
        var items = await _context.Materials.AsNoTracking().Include(m => m.Unit).OrderBy(m => m.Name).ToListAsync().ConfigureAwait(false);
        return Ok(items.Select(m => new
        {
            m.Id,
            m.Name,
            m.Quantity,
            m.AlertLimit,
            m.UnitId,
            unitName = m.Unit.Name,
            unitSymbol = m.Unit.Symbol
        }));
    }

    [HttpPost("materials")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateMaterial([FromBody] MobileMaterialWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Id and name are required." });
        }

        _context.Materials.Add(new Material
        {
            Id = request.Id.Trim(),
            Name = request.Name.Trim(),
            UnitId = request.UnitId,
            Quantity = request.Quantity,
            OriginalQuantity = request.Quantity,
            AlertLimit = request.AlertLimit,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Ok(new { ok = true });
    }

    [HttpGet("orders")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Orders()
    {
        var items = await _context.Orders.AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .OrderByDescending(o => o.OrderDate)
            .Take(200)
            .ToListAsync()
            .ConfigureAwait(false);

        return Ok(items.Select(o => new
        {
            o.OrderId,
            o.OrderNumber,
            o.OrderDate,
            userName = o.User.FullName ?? o.User.Email ?? "User",
            o.PaymentMethod,
            o.Discount,
            o.Tips,
            subtotal = o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity),
            total = o.OrderDetails.Sum(d => d.UnitPrice * d.Quantity) - o.Discount + o.Tips,
            items = o.OrderDetails.Select(d => new { d.ProductId, d.Quantity, d.UnitPrice }).ToList()
        }));
    }

    [HttpPost("orders")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> CreateOrder([FromBody] SubmitOrderRequest request)
    {
        if (request is null || request.Items.Count == 0) return BadRequest(new { message = "Order items are required." });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        var productIds = request.Items.Select(i => i.ProductId.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var products = await _context.Products.Where(p => productIds.Contains(p.Id)).ToListAsync().ConfigureAwait(false);
        if (products.Count != productIds.Count) return BadRequest(new { message = "One or more products do not exist." });
        var byId = products.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

        var order = new Order
        {
            OrderId = Guid.NewGuid().ToString("N"),
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}",
            OrderDate = DateTime.UtcNow,
            UserId = userId,
            PaymentMethod = request.PaymentMethod,
            Discount = request.Discount,
            Tips = request.Tips,
            Notes = request.Notes?.Trim(),
            OrderDetails = request.Items.Select(i => new OrderDetail
            {
                ProductId = i.ProductId.Trim(),
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice > 0 ? i.UnitPrice : byId[i.ProductId.Trim()].Price
            }).ToList(),
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Ok(new { order.OrderId, order.OrderNumber });
    }

    [HttpGet("expenses")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> Expenses()
    {
        var items = await _context.Expenses.AsNoTracking().OrderByDescending(e => e.CreatedAt).Take(300).ToListAsync().ConfigureAwait(false);
        return Ok(items.Select(e => new { e.Id, e.Title, e.Amount, e.CreatedAt, e.Notes, e.MaterialId, e.MaterialQuantity }));
    }

    [HttpPost("expenses")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> CreateExpense([FromBody] MobileExpenseWriteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Amount <= 0)
        {
            return BadRequest(new { message = "Title and positive amount are required." });
        }

        var expense = new Expense
        {
            Title = request.Title.Trim(),
            Amount = request.Amount,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreatedAt = DateTime.Now,
            MaterialId = string.IsNullOrWhiteSpace(request.MaterialId) ? null : request.MaterialId.Trim(),
            MaterialQuantity = request.MaterialQuantity,
            UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            UserName = User.Identity?.Name
        };
        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Ok(new { ok = true });
    }

    [HttpGet("reports/summary")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> ReportSummary()
    {
        var grossSales = await _context.OrderDetails.AsNoTracking().SumAsync(d => (double?)(d.UnitPrice * d.Quantity)).ConfigureAwait(false) ?? 0;
        var discount = await _context.Orders.AsNoTracking().SumAsync(o => (double?)o.Discount).ConfigureAwait(false) ?? 0;
        var expenses = await _context.Expenses.AsNoTracking().SumAsync(e => (double?)e.Amount).ConfigureAwait(false) ?? 0;
        var lowStock = await _context.Materials.AsNoTracking().CountAsync(m => m.Quantity <= m.AlertLimit).ConfigureAwait(false);
        return Ok(new
        {
            grossSales,
            totalDiscount = discount,
            totalExpenses = expenses,
            netRevenue = (grossSales - discount) - expenses,
            lowStockCount = lowStock,
        });
    }

    [HttpGet("attendance/me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> AttendanceMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var items = await _context.AttendanceRecords.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.WorkDate)
            .Take(60)
            .ToListAsync()
            .ConfigureAwait(false);
        return Ok(items.Select(a => new { a.WorkDate, a.CheckInAtUtc, a.CheckOutAtUtc }));
    }

    [HttpPost("attendance/checkout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> AttendanceCheckout()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        var today = DateTime.Today;
        var record = await _context.AttendanceRecords.FirstOrDefaultAsync(r => r.UserId == userId && r.WorkDate == today).ConfigureAwait(false);
        if (record is null) return NotFound(new { message = "No attendance record for today." });
        if (record.CheckOutAtUtc is not null) return Ok(new { ok = true });
        record.CheckOutAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Ok(new { ok = true });
    }

    [HttpGet("users")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync().ConfigureAwait(false);
        var rows = new List<object>();
        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u).ConfigureAwait(false);
            rows.Add(new { u.Id, fullName = u.FullName, u.Email, role = roles.FirstOrDefault() });
        }
        return Ok(rows);
    }

    [HttpGet("setup/profile")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> Profile()
    {
        var profile = await _context.OrganizationProfiles.AsNoTracking().OrderBy(p => p.Id).FirstOrDefaultAsync().ConfigureAwait(false);
        return Ok(new
        {
            name = profile?.Name ?? string.Empty,
            phone = profile?.Phone ?? string.Empty,
            address = profile?.Address ?? string.Empty
        });
    }

    [HttpPut("setup/profile")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AppRoles.Admin)]
    public async Task<IActionResult> UpdateProfile([FromBody] MobileProfileWriteRequest request)
    {
        var profile = await _context.OrganizationProfiles.OrderBy(p => p.Id).FirstOrDefaultAsync().ConfigureAwait(false);
        if (profile is null)
        {
            profile = new OrganizationProfile();
            _context.OrganizationProfiles.Add(profile);
        }
        profile.Name = request.Name?.Trim() ?? string.Empty;
        profile.Phone = request.Phone?.Trim() ?? string.Empty;
        profile.Address = request.Address?.Trim() ?? string.Empty;
        await _context.SaveChangesAsync().ConfigureAwait(false);
        return Ok(new { ok = true });
    }

    private string BuildToken(ApplicationUser user, IList<string> roles)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? "MagdyPOS";
        var audience = jwtSection["Audience"] ?? "MagdyPOS.Mobile";
        var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key missing.");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(12),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task EnsureCheckedInTodayAsync(string userId)
    {
        var today = DateTime.Today;
        var exists = await _context.AttendanceRecords.AnyAsync(r => r.UserId == userId && r.WorkDate == today).ConfigureAwait(false);
        if (exists) return;
        _context.AttendanceRecords.Add(new AttendanceRecord
        {
            UserId = userId,
            WorkDate = today,
            CheckInAtUtc = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}

public sealed class MobileLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class MobileProductWriteRequest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public long UnitId { get; set; }
}

public sealed class MobileUnitWriteRequest
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class MobileMaterialWriteRequest
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long UnitId { get; set; }
    public double Quantity { get; set; }
    public double AlertLimit { get; set; }
}

public sealed class MobileExpenseWriteRequest
{
    public string Title { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string? Notes { get; set; }
    public string? MaterialId { get; set; }
    public double? MaterialQuantity { get; set; }
}

public sealed class MobileProfileWriteRequest
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}
