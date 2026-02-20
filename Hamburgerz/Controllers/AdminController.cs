using Microsoft.AspNetCore.Mvc;

namespace Hamburgerz.Controllers
{
	public class AdminController : Controller
	{
		public IActionResult Dashboard()
		{
			return View();
		}
	}
}
