using Hamburgerz.Data;
using Hamburgerz.Models;
using Microsoft.AspNetCore.Identity;
using Hamburgerz.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hamburgerz.Controllers
{
    public class DataController : Controller
    {
        private readonly AppDbContext _context;

        public DataController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult DataEntry()
        {
            return View();
        }



        [HttpPost]
        public async Task<IActionResult> DataEntry(RiskData riskData)
        {
            ModelState.Remove("ID");
            ModelState.Remove("TimeStamp");
            var userId = HttpContext.Session.GetInt32("UserId");

            // Set UserId from authenticated user (example using Identity)
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user != null)
            {
                riskData.UserId = user.Id;
                riskData.Gender = user.Gender;
            }
            else
            {
                ModelState.AddModelError("", "User not found.");
                return View(riskData);
            }

            if (ModelState.IsValid)
            {
                riskData.TimeStamp = DateTime.Now;
                _context.Add(riskData);
                await _context.SaveChangesAsync();
                return RedirectToAction("Result", "Profile", new { id = riskData.ID });
            }
            return View(riskData);
        }
    }
}
