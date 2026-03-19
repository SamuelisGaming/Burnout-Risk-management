using Microsoft.AspNetCore.Mvc;

namespace Hamburgerz.Controllers
{
    public class DataController : Controller
    {
        [HttpGet]
        public IActionResult DataEntry()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SubmitData()
        {
            return RedirectToAction("Index", "Risk");
        }
    }
}