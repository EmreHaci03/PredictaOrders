using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

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
            // En pahalı satış (tek sipariş)
            var mostExpensiveSale = await _context.Orders
                .OrderByDescending(x => x.Quantity * x.Product.UnitPrice)
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


            // Müşteri başına ortalama harcama
            var totalRevenue = await _context.Orders
                .SumAsync(x => x.Quantity * x.Product.UnitPrice);

            var activeCustomerCount = await _context.Orders
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync();

            var averageSpendPerCustomer = activeCustomerCount > 0
                ? totalRevenue / activeCustomerCount
                : 0;

            ViewBag.AverageSpendPerCustomer = averageSpendPerCustomer;
            ViewBag.ActiveCustomerCount = activeCustomerCount;

            var averageQuantityPerOrder = await _context.Orders.AverageAsync(x => (double)x.Quantity);
            ViewBag.AverageQuantityPerOrder = averageQuantityPerOrder;
            var maxQuantityPerOrder = await _context.Orders.MaxAsync(x => (double)x.Quantity);
            ViewBag.MaxQuantityPerOrder = maxQuantityPerOrder;

            var today = DateTime.Today;
            var tomorrow= today.AddDays(1);
            ViewBag.TodayRevenue = await _context.Orders.Where(x => x.OrderDate >= today && x.OrderDate < tomorrow)
                 .SumAsync(x => x.Quantity * x.Product.UnitPrice);

            ViewBag.TodayOrderCount=await _context.Orders.CountAsync(x=>x.OrderDate >= today && x.OrderDate<tomorrow);

            return View();
        }
    }
}
