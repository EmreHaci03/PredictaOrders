using Microsoft.AspNetCore.Mvc;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminStatisticController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
