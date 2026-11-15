using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;

namespace BigDataOrdersDashboard.ViewComponents.AdminDashboard
{
    public class DashboardSalesOverviewViewComponent : ViewComponent
    {
        private readonly BigDataDbContext _context;

        public DashboardSalesOverviewViewComponent(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // ===== Son 12 ay: ciro + sipariş =====
            var startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-11);
            var tr = new CultureInfo("tr-TR");

            var monthlySales = await _context.Orders
                .Where(x => x.OrderDate >= startDate)
                .GroupBy(x => new { x.OrderDate.Year, x.OrderDate.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    OrderCount = g.Count(),
                    Revenue = g.Sum(x => x.Quantity * x.Product.UnitPrice)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            ViewBag.MonthlySalesLabels = JsonSerializer.Serialize(
                monthlySales.Select(x => new DateTime(x.Year, x.Month, 1).ToString("MMM yy", tr)));
            ViewBag.MonthlySalesRevenue = JsonSerializer.Serialize(monthlySales.Select(x => x.Revenue));
            ViewBag.MonthlySalesOrders = JsonSerializer.Serialize(monthlySales.Select(x => x.OrderCount));

            // ===== Sipariş durumları =====
            var orderStatus = await _context.Orders
                .GroupBy(x => x.OrderStatus)
                .Select(g => new
                {
                    OrderStatus = g.Key,
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.OrderCount)
                .ToListAsync();

            ViewBag.JsonOrderStatus = JsonSerializer.Serialize(orderStatus.Select(x => x.OrderStatus));
            ViewBag.JsonOrderCount = JsonSerializer.Serialize(orderStatus.Select(x => x.OrderCount));

            return View();
        }
    }
}