using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using System.Text.Json;
using System.Threading.Tasks;

namespace BigDataOrdersDashboard.Controllers
{
    public class CustomerAnalyticsController : Controller
    {
        private readonly BigDataDbContext _context;
        private readonly MLContext _mLContext;

        public CustomerAnalyticsController(BigDataDbContext context, MLContext mLContext)
        {
            _context = context;
            _mLContext = mLContext;
        }

        public async Task<IActionResult> Index()
        {

            var topCustomerCities = await _context
                .Orders
                .Where(x => x.Customer.CustomerCity != null && x.Customer.CustomerCity != "")
                .GroupBy(x => x.Customer.CustomerCity)
                .Select(g => new
                {
                    City = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync();

            ViewBag.TopCustomerCityName=JsonSerializer.Serialize(topCustomerCities.Select(x => x.City).ToList());
            ViewBag.TopCustomerCityCount = JsonSerializer.Serialize(topCustomerCities.Select(x => x.Count).ToList());
            return View();
        }
    }
}
