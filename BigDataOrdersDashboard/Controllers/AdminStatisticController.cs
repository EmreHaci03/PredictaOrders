using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminStatisticController : Controller
    {
        private readonly BigDataDbContext _context;

        public AdminStatisticController(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> NumericStatistics()
        {
            var totalOrder=await _context.Orders.CountAsync();
            var totalRevenue=await _context.Orders.SumAsync(x=>x.Quantity * x.Product.UnitPrice);
            var Now=DateTime.Now;
            var startOfMonth=new DateTime(Now.Year,Now.Month,1); //Yıl Ay Gün
                                                                 // 2026      11    1 

            var topCustomer = await _context.Orders
                .Include(x=>x.Customer)
                .GroupBy(x => new { x.CustomerId, x.Customer.CustomerName, x.Customer.CustomerSurname })
                .Select(g => new
                {
                    Name = g.Key.CustomerName + " " + g.Key.CustomerSurname,
                    OrderCount = g.Count()
                }).OrderByDescending(x => x.OrderCount)
                .FirstOrDefaultAsync();

            var model = new NumericStatisticsModel
            {
                TotalOrder = await _context.Orders.CountAsync(),
                TotalQuantitySold = await _context.Orders.SumAsync(x => x.Quantity),
                ThisMonthOrderCount = await _context.Orders.CountAsync(x => x.OrderDate >= startOfMonth),
                CompletedOrderCount = await _context.Orders.CountAsync(x => x.OrderStatus == "Tamamlandı"),
                PendingOrderCount = await _context.Orders.CountAsync(x => x.OrderStatus == "Beklemede"),
                CancelledOrderCount = await _context.Orders.CountAsync(x => x.OrderStatus == "İptal Edildi"),
                ShippedOrderCount = await _context.Orders.CountAsync(x => x.OrderStatus == "Kargoda"),
                TotalProductCount = await _context.Products.CountAsync(),
                TotalCategoryCount = await _context.Categories.CountAsync(),
                MaxPriceProductName = await _context.Products.OrderByDescending(x => x.UnitPrice).Select(x => x.ProductName).FirstOrDefaultAsync(),
                MaxProductPrice = await _context.Products.MaxAsync(x => x.UnitPrice),
                MinPriceProductName = await _context.Products.OrderBy(x => x.UnitPrice).Select(x => x.ProductName).FirstOrDefaultAsync(),
                MinProductPrice = await _context.Products.MinAsync(x => x.UnitPrice),
                AverageProductPrice = await _context.Products.AverageAsync(x => x.UnitPrice),
                TotalQuantityCount = await _context.Products.SumAsync(x => x.StockQuantity),
                CriticalStockProductCount = await _context.Products.CountAsync(x => x.StockQuantity < 50),
                TotalCustomerCount = await _context.Customers.CountAsync(),
                DistinctCountryCount = await _context.Customers.Select(x => x.CustomerCountry).Distinct().CountAsync(),
                InactiveCustomerCount = await _context.Customers.CountAsync(x => !_context.Orders.Any(o => o.CustomerId == x.CustomerId)),
                TotalStockValue = await _context.Products.SumAsync(x => x.UnitPrice * x.StockQuantity),
                TotalRevenue = await _context.Orders.SumAsync(x => x.Quantity * x.Product.UnitPrice),
                AverageBasket = totalOrder > 0 ? totalRevenue / totalOrder : 0,
                TopCustomerName = topCustomer.Name,
                TopCustomerOrderCount= topCustomer.OrderCount,

            };
            return View(model);
        }


    }
}
