using MagdyPOS.Authorization;
using MagdyPOS.Data;
using MagdyPOS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MagdyPOS.Controllers
{
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Sales}")]
    public class ReturnController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReturnController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 20)
        {
            if (from == null)
                from = DateTime.Now.AddDays(-1);

            if (to == null)
                to = DateTime.Now;

            if (User.IsInRole(AppRoles.Admin))
            {
                if (from > to)
                {
                    ViewBag.DateFilterError = "تاريخ البداية بعد تاريخ النهاية";
                    return View(new List<OrderListItemViewModel>());
                }

                ViewData["Title"] = "المرتجعات";
                var allReturns = await _context.Returns
                .AsNoTracking()
                .Include(o => o.Order)
                .Include(o => o.User)
                .Where(o => o.Order.OrderDate >= from && o.Order.OrderDate <= to)
                .OrderByDescending(o => o.Order.OrderDate)
                .Skip((pageNo - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new ReturnViewModel
                {
                    Amount = o.Amount,
                    Notes = o.Notes,
                    Order = o.Order,
                    User = o.User,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync()
                .ConfigureAwait(false);

                return View(allReturns);
            }

            ViewData["Title"] = "مرتجعاتي";
            var userId = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier);

            var myReturns = await _context.Returns
                .Where(o => o.UserId == userId.Value)
                .AsNoTracking()
                .Include(o => o.Order)
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((pageNo - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new ReturnViewModel
                {
                    Amount = o.Amount,
                    Notes = o.Notes,
                    Order = o.Order,
                    CreatedAt = o.CreatedAt,
                    User = o.User,
                })
                .ToListAsync()
                .ConfigureAwait(false);

            return View(myReturns);
        }

        [HttpPost]
        public async Task<IActionResult> Create(AddNewReturnModel model)
        {
            try
            {
                var errors = ValidateReturn(model);
                if (errors.Any())
                {
                    return BadRequest(errors);
                }

                var userId = User.Claims.FirstOrDefault(u => u.Type == ClaimTypes.NameIdentifier)?.Value;

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == model.OrderId);

                if (order == null)
                {
                    return BadRequest(new List<string> { "رقم الطلب غير موجود" });
                }

                order.State = OrderState.Returned;
                _context.Orders.Update(order);

                var newReturn = new Return
                {
                    Amount = model.Amount,
                    Notes = model.Notes,
                    OrderId = order.OrderId,
                    UserId = userId
                };

                await _context.Returns.AddAsync(newReturn);
                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new List<string> { "حدث خطأ أثناء معالجة الطلب", ex.Message });
            }
        }

        private List<string> ValidateReturn(AddNewReturnModel model)
        {
            var errors = new List<string>();
            if (model.Amount <= 0)
                errors.Add("المبلغ يجب أن يكون أكبر من صفر");
            if (string.IsNullOrEmpty(model.Notes))
                errors.Add("الملاحظات مطلوبة");
            if (string.IsNullOrEmpty(model.OrderId))
                errors.Add("رقم الطلب مطلوب");
            return errors;
        }
    }
}
