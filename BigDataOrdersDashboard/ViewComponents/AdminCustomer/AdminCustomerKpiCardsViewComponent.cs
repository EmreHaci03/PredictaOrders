using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.ViewComponents.AdminCustomer
{
    public class AdminCustomerKpiCardsViewComponent:ViewComponent
    {
        private readonly BigDataDbContext _context;

        public AdminCustomerKpiCardsViewComponent(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int customerId)
        {
            var referanceDate = new DateTime(2026, 1,1);

            var stats = await _context.Orders
                .Where(x => x.CustomerId == customerId)
                .GroupBy(x => x.CustomerId)
                .Select(g => new CustomerKpiViewModel
                {
                    TotalOrderCount=g.Count(),
                    TotalSpend=g.Sum(x=>x.Quantity*x.Product.UnitPrice),
                    LastOrderDate=g.Max(x=>x.OrderDate),
                    DaysSinceLastOrder=EF.Functions.DateDiffDay(g.Max(x=>x.OrderDate),referanceDate),
                    UniqueProductCount=g.Select(x=>x.ProductId).Distinct().Count()
                })
                .FirstOrDefaultAsync() ?? new CustomerKpiViewModel();

            stats.AverageBasketAmount = stats.TotalOrderCount > 0
                ? stats.TotalSpend / stats.TotalOrderCount : 0;

            return View(stats);
        }
    }
}
