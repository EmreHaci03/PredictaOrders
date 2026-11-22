using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BigDataOrdersDashboard.ViewComponents.AdminDashboard
{
    public class DashboardRecentActivityViewComponent : ViewComponent
    {
        private readonly BigDataDbContext _context;
        private readonly IMemoryCache _cache;

        public DashboardRecentActivityViewComponent(BigDataDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var lastOrders = await _cache.GetOrCreateAsync("Dashboard_LastOrders", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                return await _context.Orders
                    .OrderByDescending(x => x.OrderDate)
                    .Take(5)
                    .Select(x => new
                    {
                        x.OrderId,
                        NameSurname = x.Customer.CustomerName + " " + x.Customer.CustomerSurname,
                        ProductName = x.Product.ProductName,
                        Quantity = x.Quantity,
                        Amount = x.Quantity * x.Product.UnitPrice,
                        Status = x.OrderStatus,
                        Date = x.OrderDate
                    })
                    .ToListAsync();
            });

            ViewBag.LastOrders = lastOrders;

            var topSpenders = await _cache.GetOrCreateAsync("Dashboard_TopSpenders", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                var topIds = await _context.Orders
                    .Select(x => new
                    {
                        x.CustomerId,
                        Amount = x.Quantity * x.Product.UnitPrice
                    })
                    .GroupBy(x => x.CustomerId)
                    .Select(g => new
                    {
                        CustomerId = g.Key,
                        Sum = g.Sum(x => x.Amount)
                    })
                    .OrderByDescending(x => x.Sum)
                    .Take(5)
                    .ToListAsync();

                var ids = topIds.Select(x => x.CustomerId).ToList();

                var customers = await _context.Customers
                    .Where(x => ids.Contains(x.CustomerId))
                    .Select(x => new { x.CustomerId, x.CustomerName, x.CustomerSurname, x.CustomerCountry })
                    .ToDictionaryAsync(x => x.CustomerId);

                return topIds.Select(x => new
                {
                    Name = customers[x.CustomerId].CustomerName,
                    Surname = customers[x.CustomerId].CustomerSurname,
                    Country = customers[x.CustomerId].CustomerCountry,
                    x.Sum
                }).ToList();
            });

            ViewBag.TopSpenders = topSpenders;
            return View();
        }
    }
}