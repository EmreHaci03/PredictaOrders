using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminDashboardController : Controller
    {
        private readonly BigDataDbContext _context;

        public AdminDashboardController(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {

            var productStats = await _context.Orders
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    MaxQuantity = g.Max(x => x.Quantity)
                })
                .ToListAsync();

            var prices = await _context.Products
                .ToDictionaryAsync(x => x.ProductId, x => x.UnitPrice);

            var totalRevenue = productStats.Sum(x => x.TotalQuantity * prices[x.ProductId]);


            var best = productStats
                .Select(x => new { x.ProductId, x.MaxQuantity, Amount = x.MaxQuantity * prices[x.ProductId] })
                .OrderByDescending(x => x.Amount)
                .FirstOrDefault();


            var mostExpensiveSale = best == null ? null : await _context.Orders
                .Where(x => x.ProductId == best.ProductId && x.Quantity == best.MaxQuantity)
                .Select(x => new
                {
                    NameSurname = x.Customer.CustomerName + " " + x.Customer.CustomerSurname,
                    Product = x.Product.ProductName,
                    Quantity = x.Quantity,
                    Amount = x.Quantity * x.Product.UnitPrice
                })
                .FirstOrDefaultAsync();

            ViewBag.MostExpensiveSaleCustomer = mostExpensiveSale?.NameSurname;
            ViewBag.MostExpensiveSaleProduct = mostExpensiveSale?.Product;
            ViewBag.MostExpensiveSaleQuantity = mostExpensiveSale?.Quantity ?? 0;
            ViewBag.MostExpensiveSaleAmount = mostExpensiveSale?.Amount ?? 0;


            var activeCustomerCount = await _context.Orders
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync();

            ViewBag.AverageSpendPerCustomer = activeCustomerCount > 0
                ? totalRevenue / activeCustomerCount
                : 0;
            ViewBag.ActiveCustomerCount = activeCustomerCount;
            ViewBag.AverageQuantityPerOrder = await _context.Orders.AverageAsync(x => (double)x.Quantity);
            ViewBag.MaxQuantityPerOrder = productStats.Any() ? productStats.Max(x => x.MaxQuantity) : 0;



            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            ViewBag.TodayRevenue = await _context.Orders
                .Where(x => x.OrderDate >= today && x.OrderDate < tomorrow)
                .SumAsync(x => x.Quantity * x.Product.UnitPrice);

            ViewBag.TodayOrderCount = await _context.Orders
                .CountAsync(x => x.OrderDate >= today && x.OrderDate < tomorrow);

            return View();
        }
    }
}