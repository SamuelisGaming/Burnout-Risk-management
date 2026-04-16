using Hamburgerz.Services;
using Microsoft.AspNetCore.Mvc;

namespace Hamburgerz.Controllers
{
    public class JobRolesController : Controller
    {
        private readonly JobRoleCatalogService _jobRoleCatalog;

        public JobRolesController(JobRoleCatalogService jobRoleCatalog)
        {
            _jobRoleCatalog = jobRoleCatalog;
        }

        [HttpGet("/api/job-roles")]
        public IActionResult Search(string? q)
        {
            var suggestions = _jobRoleCatalog.Search(q, maxResults: 8)
                .Select(item => new
                {
                    canonicalTitle = item.CanonicalTitle
                });

            return Json(suggestions);
        }
    }
}
