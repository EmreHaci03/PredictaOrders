using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BigDataOrdersDashboard.ViewComponents.AdminDashboard
{
    public class DashboardDistributionsViewComponent:ViewComponent
    {
        private readonly BigDataDbContext _context;

        public DashboardDistributionsViewComponent(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {

            var categoryRevenue = await _context.Orders
                .GroupBy(x => new { x.Product.Category.CategoryId, x.Product.Category.CategoryName })
                .Select(g => new
                {
                    CategoryName = g.Key.CategoryName,
                    Revenue = g.Sum(x => x.Quantity * x.Product.UnitPrice)
                }).OrderByDescending(x => x.Revenue)
                .Take(6)
                .ToListAsync();
            ViewBag.CategoryName = JsonSerializer.Serialize(categoryRevenue.Select(x=>x.CategoryName));
            ViewBag.CategoryRevenue = JsonSerializer.Serialize(categoryRevenue.Select(x => x.Revenue));


            var countryOrders = await _context.Orders
                .GroupBy(x =>x.Customer.CustomerCountry )
                .Select(g => new
                {
                    Country = g.Key,
                    OrderCount = g.Count()
                }).OrderByDescending(x => x.OrderCount)
                .Take(6)
                .ToListAsync();

            ViewBag.Country=JsonSerializer.Serialize(countryOrders.Select(x=>x.Country));   
            ViewBag.CountryOrder=JsonSerializer.Serialize(countryOrders.Select(x=>x.OrderCount));

            var paymentMethod=await _context.Orders
                .GroupBy(x=>x.PaymentMethod)
                .Select(g => new
                {
                    paymentMethod = g.Key,  
                    Count=g.Count()
                }).OrderByDescending(x=>x.Count)
                .Take(5)
                .ToListAsync();

            ViewBag.PaymentMethod = JsonSerializer.Serialize(paymentMethod.Select(x => x.paymentMethod));
            ViewBag.PaymentCount=JsonSerializer.Serialize(paymentMethod.Select(x=>x.Count));
            return View();
        }
    }
}
