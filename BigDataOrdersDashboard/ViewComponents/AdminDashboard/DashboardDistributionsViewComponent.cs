using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BigDataOrdersDashboard.ViewComponents.AdminDashboard
{
    public class DashboardDistributionsViewComponent : ViewComponent
    {
        private readonly BigDataDbContext _context;

        public DashboardDistributionsViewComponent(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // ===== 1) KATEGORİ CİROSU =====

            // Ürün bazında toplam adet (JOIN yok, hızlı)
            var productQuantities = await _context.Orders
                .GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToListAsync();

            // Ürünlerin fiyat ve kategori bilgisi
            var products = await _context.Products
                .Select(x => new { x.ProductId, x.UnitPrice, x.CategoryId, x.Category.CategoryName })
                .ToDictionaryAsync(x => x.ProductId);

            // Kategori cirosu hafızada hesaplanıyor
            var categoryRevenue = productQuantities
                .GroupBy(x => new { products[x.ProductId].CategoryId, products[x.ProductId].CategoryName })
                .Select(g => new
                {
                    CategoryName = g.Key.CategoryName,
                    Revenue = g.Sum(x => x.Quantity * products[x.ProductId].UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(6)
                .ToList();

            ViewBag.CategoryName = JsonSerializer.Serialize(categoryRevenue.Select(x => x.CategoryName));
            ViewBag.CategoryRevenue = JsonSerializer.Serialize(categoryRevenue.Select(x => x.Revenue));

            // ===== 2) ÜLKELERE GÖRE SİPARİŞ =====

            // Müşteri bazında sipariş sayısı (JOIN yok, hızlı)
            var customerOrderCounts = await _context.Orders
                .GroupBy(x => x.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .ToListAsync();

            // Müşterilerin ülkeleri
            var customerCountries = await _context.Customers
    .Select(x => new { x.CustomerId, x.CustomerCountry })
    .ToDictionaryAsync(x => x.CustomerId, x => x.CustomerCountry);

            // Ülke toplamları hafızada hesaplanıyor
            var countryOrders = customerOrderCounts
                .GroupBy(x => customerCountries[x.CustomerId])
                .Select(g => new { Country = g.Key, OrderCount = g.Sum(x => x.Count) })
                .OrderByDescending(x => x.OrderCount)
                .Take(6)
                .ToList();

            ViewBag.Country = JsonSerializer.Serialize(countryOrders.Select(x => x.Country));
            ViewBag.CountryOrder = JsonSerializer.Serialize(countryOrders.Select(x => x.OrderCount));

            // ===== 3) ÖDEME YÖNTEMLERİ =====

            var paymentMethod = await _context.Orders
                .GroupBy(x => x.PaymentMethod)
                .Select(g => new { PaymentMethod = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            ViewBag.PaymentMethod = JsonSerializer.Serialize(paymentMethod.Select(x => x.PaymentMethod));
            ViewBag.PaymentCount = JsonSerializer.Serialize(paymentMethod.Select(x => x.Count));

            return View();
        }
    }
}