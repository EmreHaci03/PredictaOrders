using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminOrderController : Controller
    {
        private readonly BigDataDbContext _context;

        public AdminOrderController(BigDataDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> Index(int page=1)
        {
            if (page < 1)
                page = 1;
            var pageSize = 100;
            var totalCount=await _context.Orders.CountAsync();
            var values = await _context.Orders
                .Include(x => x.Customer)
                .Include(x => x.Product)
                .OrderByDescending(x => x.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            ViewBag.TotalCount= totalCount;
            ViewBag.CurrentPage= page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(values);
        }
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term)
        {
            var values = await _context.Products
                .Where(x => x.ProductName.Contains(term))
                .OrderBy(x => x.ProductName)
                .Take(20)
                .Select(x => new { id = x.ProductId, text = x.ProductName ,price=x.UnitPrice})
                .ToListAsync();

            return Json(values); // Scriptten Çağırılacağı İçin Json Döndürüyoruz
        }

        [HttpGet]
        public async Task<IActionResult> SearchCustomers(string term)
        {
            var values=await _context.Customers
                .Where(x=>x.CustomerName.Contains(term) || x.CustomerSurname.Contains(term))
                .OrderBy(x=>x.CustomerName)
                .Take(20)
        .Select(x => new { id = x.CustomerId, text = x.CustomerName + " " + x.CustomerSurname, sub = x.CustomerEmail })
                .ToListAsync(); 

            return Json(values);
        }

        [HttpGet]
        public IActionResult CreateOrder()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateOrder(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateOrder(int id)
        {
            var value = await _context.Orders
                .Include(x => x.Product)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.OrderId == id);
            if (value == null)
                return NotFound();

            return View(value);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateOrder(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var value = await _context.Orders.FindAsync(id);
            if(value == null)
                return NotFound();

            _context.Orders.Remove(value);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));

        }
    }
}
