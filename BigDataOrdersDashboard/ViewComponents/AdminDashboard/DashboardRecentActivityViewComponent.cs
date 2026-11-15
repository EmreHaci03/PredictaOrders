using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.ViewComponents.AdminDashboard
{
    public class DashboardRecentActivityViewComponent:ViewComponent
    {
        private readonly BigDataDbContext _context;

        public DashboardRecentActivityViewComponent(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var lastOrders = await _context.Orders
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

            ViewBag.LastOrders = lastOrders;

            var topSpenders = await _context.Orders
                .GroupBy(x => new { x.CustomerId, x.Customer.CustomerName, x.Customer.CustomerSurname ,x.Customer.CustomerCountry})
                .Select(g => new
                {
                    Name= g.Key.CustomerName,
                    Surname=g.Key.CustomerSurname,
                    Country=g.Key.CustomerCountry,
                    Sum = g.Sum(x=>x.Quantity * x.Product.UnitPrice),
                }).OrderByDescending(x => x.Sum)
                .Take(5)
                .ToListAsync();


            ViewBag.TopSpenders = topSpenders;
            return View();
        }
    }
}

