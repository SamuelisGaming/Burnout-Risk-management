using Microsoft.AspNetCore.Mvc;

namespace Hamburgerz.Controllers
{
	public class ProfileController : Controller
	{
		public IActionResult History()
		{
			return View();
		}
	}
}
