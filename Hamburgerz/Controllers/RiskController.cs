using Microsoft.AspNetCore.Mvc;

namespace Hamburgerz.Controllers
{
    public class RiskController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction("DataEntry", "Data");
        }
    }
}
