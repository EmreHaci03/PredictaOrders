using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.ViewComponents.CustomerAnalytics
{
    public class CustomerAnalyticsStatsViewComponent : ViewComponent
    {
        private readonly BigDataDbContext _context;

        public CustomerAnalyticsStatsViewComponent(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var referanceDate = new DateTime(2026, 1, 1);


            // Müşteri başına sipariş
            var totalOrders = await _context.Orders
                .CountAsync(x => x.OrderDate < referanceDate);

            var orderingCustomers = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync();

            ViewBag.OrdersPerCustomer = orderingCustomers > 0
                ? totalOrders * 1.0 / orderingCustomers     
                : 0.0;

            ViewBag.MostPopulatedCity = await _context.Customers
                .Where(x => x.CustomerCity != null && x.CustomerCity != "")
                .GroupBy(x => x.CustomerCity)
                .Select(g => new { City = g.Key, CustomerCount = g.Count() })
                .OrderByDescending(x => x.CustomerCount)
                .FirstOrDefaultAsync();

            ViewBag.MissingEmailCustomers = await _context.Customers
                .CountAsync(x => x.CustomerEmail == null || x.CustomerEmail == "");

            ViewBag.CashOnDeliveryCustomers = await _context.Orders
                .Where(x => x.PaymentMethod == "Kapıda Ödeme" && x.OrderDate < referanceDate)
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync();



            ViewBag.OneTimeCustomers = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .GroupBy(x => x.CustomerId)
                .Where(g => g.Count() == 1)                          
                .CountAsync();


            var start2025 = new DateTime(2025, 1, 1);

            ViewBag.acquiredIn2025 = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .GroupBy(x => x.CustomerId)
                .Where(g => g.Min(o => o.OrderDate) >= start2025)           
                .CountAsync();



            ViewBag.multiPaymentCustomers = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .GroupBy(x => x.CustomerId)
                .Where(g => g.Select(x => x.PaymentMethod).Distinct().Count() >= 3)
                .CountAsync();



            ViewBag.TopQuantityCustomer = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .GroupBy(x => new { x.CustomerId, x.Customer.CustomerName, x.Customer.CustomerSurname })
                .Select(g => new
                {
                    NameSurname = g.Key.CustomerName + " " + g.Key.CustomerSurname,
                    Sum = g.Sum(x => x.Quantity)
                })
                .OrderByDescending(x => x.Sum)
                .FirstOrDefaultAsync();



            ViewBag.TopCancellingCustomer = await _context.Orders
                .Where(x => x.OrderStatus == "İptal Edildi" && x.OrderDate < referanceDate)
                .GroupBy(x => new { x.CustomerId, x.Customer.CustomerName, x.Customer.CustomerSurname })
                .Select(g => new
                {
                    NameSurname = g.Key.CustomerName + " " + g.Key.CustomerSurname,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();



            ViewBag.MostCuriousCustomer = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .GroupBy(x => new { x.CustomerId, x.Customer.CustomerName, x.Customer.CustomerSurname })
                .Select(g => new
                {
                    NameSurname = g.Key.CustomerName + " " + g.Key.CustomerSurname,
                    ProductCount = g.Select(x => x.ProductId).Distinct().Count()
                })
                .OrderByDescending(x => x.ProductCount)
                .FirstOrDefaultAsync();




            var driftLimit = referanceDate.AddDays(-180);

            ViewBag.DriftingTopCustomer = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .GroupBy(x => new { x.CustomerId, x.Customer.CustomerName, x.Customer.CustomerSurname })
                .Where(g => g.Max(o => o.OrderDate) < driftLimit)           
                .Select(g => new
                {
                    NameSurname = g.Key.CustomerName + " " + g.Key.CustomerSurname,
                    TotalSpent = g.Sum(x => x.Quantity * x.Product.UnitPrice),
                    DaysAway = EF.Functions.DateDiffDay(g.Max(x => x.OrderDate), referanceDate)
                })
                .OrderByDescending(x => x.TotalSpent)
                .FirstOrDefaultAsync();



            var lastWeek = referanceDate.AddDays(-7);

            ViewBag.LastWeekCustomers = await _context.Orders
                .Where(x => x.OrderDate >= lastWeek && x.OrderDate < referanceDate)
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync();

            return View();
        }
    }
}