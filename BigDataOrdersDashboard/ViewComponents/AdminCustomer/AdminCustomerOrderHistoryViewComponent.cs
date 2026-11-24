using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.ViewComponents.AdminCustomer
{
    public class AdminCustomerOrderHistoryViewComponent:ViewComponent
    {
        private readonly BigDataDbContext _context;

        public AdminCustomerOrderHistoryViewComponent(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int customerId)
        {
            ViewBag.Last8Orders = await _context.Orders.Where(x => x.CustomerId == customerId)
                .Select(g => new
                {
                    OrderId=g.OrderId,
                    Product=g.Product.ProductName,
                    Status=g.OrderStatus,
                    OrderDate = g.OrderDate,
                    PaymentMethod=g.PaymentMethod,
                    Quantity = g.Quantity,
                    Amount =(g.Quantity*g.Product.UnitPrice)
                })
                .OrderByDescending(x => x.OrderDate).Take(5).ToListAsync();

            ViewBag.Last8OrderReviews=await _context.Reviews.Where(x=>x.CustomerId==customerId)
                .Select(g => new
                {
                    Product=g.Product.ProductName,
                    PurchaseType=g.PurchaseType,
                    Sentiment = g.Sentiment,
                    Rating = g.Rating,
                    ReviewText=g.ReviewText,
                    Date=g.ReviewDate
                }).OrderByDescending(g=>g.Date)
                .Take(5).ToListAsync(); 

            return View();
        }
    }
}
