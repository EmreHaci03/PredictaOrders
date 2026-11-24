using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BigDataOrdersDashboard.ViewComponents.AdminCustomer
{
    public class AdminCustomerPurchaseInsightsViewComponent:ViewComponent
    {
        private readonly BigDataDbContext _context;

        public AdminCustomerPurchaseInsightsViewComponent(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int customerId)
        {

            var FavoriteProducts = await _context.Orders
                .Where(x => x.CustomerId == customerId)
                .GroupBy(x => new { x.Product.ProductName, x.Product.Category.CategoryName })
                .Select(g => new
                {
                    Product = g.Key.ProductName,
                    Category = g.Key.CategoryName,
                    Count = g.Count()
                }).OrderByDescending(x => x.Count).Take(5).ToListAsync();

            ViewBag.FavoriteProducts = FavoriteProducts;


            var OrderByCategory = await _context.Orders.Where(x => x.CustomerId == customerId)
                .Include(x => x.Product)
                .ThenInclude(x => x.Category)
                .GroupBy(x => x.Product.Category.CategoryName)
                .Select(g => new
                {
                    Category = g.Key,
                    Spend = g.Sum(x => x.Quantity * x.Product.UnitPrice)
                }).OrderByDescending(x => x.Spend).ToListAsync();

            ViewBag.CategoryName = JsonSerializer.Serialize(OrderByCategory.Select(x => x.Category));
            ViewBag.CategorySpend = JsonSerializer.Serialize(OrderByCategory.Select(x => x.Spend));

            var Payments = await _context.Orders.Where(x => x.CustomerId == customerId)
                .GroupBy(x => x.PaymentMethod)
                .Select(g => new
                {
                    Payment = g.Key,
                    Count = g.Count(),
                }).OrderByDescending(x => x.Count).ToListAsync();

            ViewBag.Payments = JsonSerializer.Serialize(Payments.Select(x => x.Payment));
            ViewBag.PaymentCount = JsonSerializer.Serialize(Payments.Select(x => x.Count));
            return View();
        }
    }
}
