using Microsoft.AspNetCore.Mvc;

namespace Hamburgerz.Controllers
{
    public class DataController : Controller
    {
        public IActionResult DataEntry()
        {
            return View();
        }

        public IActionResult SubmitData()
        {
            return View();
        }
    }
}
