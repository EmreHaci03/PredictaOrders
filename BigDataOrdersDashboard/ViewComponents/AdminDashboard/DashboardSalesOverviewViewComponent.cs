using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.Json;

namespace BigDataOrdersDashboard.ViewComponents.AdminDashboard
{
    public class DashboardSalesOverviewViewComponent : ViewComponent
    {
        private readonly BigDataDbContext _context;
        private readonly IMemoryCache _cache;

        public DashboardSalesOverviewViewComponent(BigDataDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // ===== Son 12 ay: ciro + sipariş =====
            var startDate = new DateTime(2026, 1, 1).AddMonths(-11);
            var tr = new CultureInfo("tr-TR");

            var monthlySales = await _cache.GetOrCreateAsync("Dashboard_MonthlySales", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                return await _context.Orders
                    .Where(x => x.OrderDate >= startDate)
                    .Select(x => new
                    {
                        x.OrderDate.Year,
                        x.OrderDate.Month,
                        Amount = x.Quantity * x.Product.UnitPrice
                    })
                    .GroupBy(x => new { x.Year, x.Month })
                    .Select(g => new
                    {
                        g.Key.Year,
                        g.Key.Month,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(x => x.Amount)
                    })
                    .ToListAsync();
            });

            // Boş ayları 0 ile doldur
            var labels = new List<string>();
            var revenues = new List<decimal>();
            var orders = new List<int>();

            for (var m = startDate; m <= startDate.AddMonths(11); m = m.AddMonths(1))
            {
                var month = monthlySales!.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month);

                labels.Add(m.ToString("MMM yy", tr));
                revenues.Add(month?.Revenue ?? 0);
                orders.Add(month?.OrderCount ?? 0);
            }

            ViewBag.MonthlySalesLabels = JsonSerializer.Serialize(labels);
            ViewBag.MonthlySalesRevenue = JsonSerializer.Serialize(revenues);
            ViewBag.MonthlySalesOrders = JsonSerializer.Serialize(orders);

            // ===== Sipariş durumları =====
            var orderStatus = await _cache.GetOrCreateAsync("Dashboard_OrderStatus", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);

                return await _context.Orders
                    .GroupBy(x => x.OrderStatus)
                    .Select(g => new
                    {
                        OrderStatus = g.Key,
                        OrderCount = g.Count()
                    })
                    .OrderByDescending(x => x.OrderCount)
                    .ToListAsync();
            });

            ViewBag.JsonOrderStatus = JsonSerializer.Serialize(orderStatus!.Select(x => x.OrderStatus));
            ViewBag.JsonOrderCount = JsonSerializer.Serialize(orderStatus!.Select(x => x.OrderCount));

            return View();
        }
    }
}