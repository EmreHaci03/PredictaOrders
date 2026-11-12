using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminCustomerController : Controller
    {
        private readonly BigDataDbContext _context;

        public AdminCustomerController(BigDataDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> Index(int page=1)
        {
            if (page < 1)
                page = 1;

            var pageSize = 100;

            var totalCount = await _context.Customers.CountAsync();

            var values = await _context.Customers.OrderBy(x => x.CustomerId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.CountryCount = await _context.Customers.Select(x => x.CustomerCountry).Distinct().CountAsync();
            ViewBag.TotalCount = totalCount;
            ViewBag.CityCount = await _context.Customers.Select(x => x.CustomerCity).Distinct().CountAsync();
            return View(values);
        }

        [HttpGet]
        public IActionResult CreateCustomer()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(Customer customer)
        {
            await _context.Customers.AddAsync(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var values = await _context.Customers.FindAsync(id);
            if (values == null)
                return RedirectToAction(nameof(Index));

             _context.Customers.Remove(values);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCustomer(int id)
        {
            var values = await _context.Customers.FindAsync(id);
            if (values == null)
                return RedirectToAction(nameof(Index));

            return View(values);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateCustomer(Customer customer)
        {
            _context.Customers.Update(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
