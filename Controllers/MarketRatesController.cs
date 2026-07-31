using System;
using System.Threading.Tasks;
using ElectroScanAI.Models.Entities;
using Electroscann_ai.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Electroscann_ai.Controllers
{
    [Authorize(Roles = "Admin")]
    public class MarketRatesController : Controller
    {
        private readonly ElectroscannDbContext _context;

        public MarketRatesController(ElectroscannDbContext context)
        {
            _context = context;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdClaim, out int userId) && userId > 0)
            {
                ViewBag.UnreadNotifications = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            }
            else
            {
                ViewBag.UnreadNotifications = 0;
            }

            await base.OnActionExecutionAsync(context, next);
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.ActivePage = "MarketRates";
            var rates = await _context.MarketRates.ToListAsync();
            return View(rates);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MarketRate model)
        {
            if (ModelState.IsValid)
            {
                model.CreatedAt = DateTime.UtcNow;
                _context.MarketRates.Add(model);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Market rate item added successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MarketRate model)
        {
            var rate = await _context.MarketRates.FindAsync(model.Id);
            if (rate == null) return NotFound();

            rate.ItemName = model.ItemName;
            rate.Category = model.Category;
            rate.Unit = model.Unit;
            rate.Price = model.Price;
            rate.City = model.City;
            rate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Market rate updated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var rate = await _context.MarketRates.FindAsync(id);
            if (rate != null)
            {
                _context.MarketRates.Remove(rate);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Market rate item deleted.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
