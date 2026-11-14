using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminStatisticController : Controller
    {
        private readonly BigDataDbContext _context;

        public AdminStatisticController(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> NumericStatistics()
        {
            var totalOrder = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders.SumAsync(x => x.Quantity * x.Product.UnitPrice);
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            // En sadık müşteri: önce Id ile grupla, sonra adı çek
            var topCustomerRow = await _context.Orders
                .GroupBy(x => x.CustomerId)
                .Select(g => new { CustomerId = g.Key, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .FirstOrDefaultAsync();

            string? topCustomerName = null;
            if (topCustomerRow != null)
            {
                topCustomerName = await _context.Customers
                    .Where(x => x.CustomerId == topCustomerRow.CustomerId)
                    .Select(x => x.CustomerName + " " + x.CustomerSurname)
                    .FirstOrDefaultAsync();
            }

            var model = new NumericStatisticsModel
            {
                // Siparişler
                TotalOrder = totalOrder,
                TotalRevenue = totalRevenue,
                AverageBasket = totalOrder > 0 ? totalRevenue / totalOrder : 0,
                TotalQuantitySold = await _context.Orders.SumAsync(x => x.Quantity),
                ThisMonthOrderCount = await _context.Orders.CountAsync(x => x.OrderDate >= startOfMonth),
                CompletedOrderCount = await _context.Orders.CountAsync(x => x.OrderStatus == "Tamamlandı"),
                PendingOrderCount = await _context.Orders.CountAsync(x => x.OrderStatus == "Beklemede"),
                CancelledOrderCount = await _context.Orders.CountAsync(x => x.OrderStatus == "İptal Edildi"),
                ShippedOrderCount = await _context.Orders.CountAsync(x => x.OrderStatus == "Kargoda"),

                // Ürünler
                TotalProductCount = await _context.Products.CountAsync(),
                TotalCategoryCount = await _context.Categories.CountAsync(),
                MaxPriceProductName = await _context.Products.OrderByDescending(x => x.UnitPrice).Select(x => x.ProductName).FirstOrDefaultAsync(),
                MaxProductPrice = await _context.Products.MaxAsync(x => x.UnitPrice),
                MinPriceProductName = await _context.Products.OrderBy(x => x.UnitPrice).Select(x => x.ProductName).FirstOrDefaultAsync(),
                MinProductPrice = await _context.Products.MinAsync(x => x.UnitPrice),
                AverageProductPrice = await _context.Products.AverageAsync(x => x.UnitPrice),
                TotalQuantityCount = await _context.Products.SumAsync(x => x.StockQuantity),
                TotalStockValue = await _context.Products.SumAsync(x => x.UnitPrice * x.StockQuantity),
                CriticalStockProductCount = await _context.Products.CountAsync(x => x.StockQuantity < 50),

                // Müşteriler
                TotalCustomerCount = await _context.Customers.CountAsync(),
                DistinctCountryCount = await _context.Customers.Select(x => x.CustomerCountry).Distinct().CountAsync(),
                InactiveCustomerCount = await _context.Customers.CountAsync(x => !_context.Orders.Any(o => o.CustomerId == x.CustomerId)),
                TopCustomerName = topCustomerName,
                TopCustomerOrderCount = topCustomerRow?.OrderCount ?? 0
            };

            return View(model);
        }

        public async Task<IActionResult> TextualStatistics()
        {
            // ===== ÜRÜNLER =====

            // En çok satan 5 ürün (tamamlanan siparişler, adet) + birincisi kart için
            var topSellingRows = await _context.Orders
                .Where(x => x.OrderStatus == "Tamamlandı")
                .GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, Count = g.Sum(x => x.Quantity) })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var productIds = topSellingRows.Select(x => x.ProductId).ToList();
            var productNames = await _context.Products
                .Where(x => productIds.Contains(x.ProductId))
                .ToDictionaryAsync(x => x.ProductId, x => x.ProductName);

            var topSellingProducts = topSellingRows
                .Select(x => new { x.ProductId, Name = productNames[x.ProductId], x.Count })
                .ToList();

            ViewBag.TopSellingProductList = topSellingProducts;
            ViewBag.TopProductName = topSellingProducts.FirstOrDefault()?.Name;
            ViewBag.TopProductQuantity = topSellingProducts.FirstOrDefault()?.Count ?? 0;

            // En çok kazandıran ürün (fiyat × toplam adet)
            var topRevenueProduct = await _context.Orders
                .GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) })
                .Join(_context.Products, t => t.ProductId, p => p.ProductId,
                      (t, p) => new { p.ProductName, ProductAmount = p.UnitPrice * t.TotalQuantity })
                .OrderByDescending(x => x.ProductAmount)
                .FirstOrDefaultAsync();

            ViewBag.TopRevenueProductName = topRevenueProduct?.ProductName;
            ViewBag.TopRevenueProductAmount = topRevenueProduct?.ProductAmount ?? 0;

            // En popüler kategori
            var mostPopularCategory = await _context.Orders
                .GroupBy(x => new { x.Product.CategoryId, x.Product.Category.CategoryName })
                .Select(g => new { CategoryName = g.Key.CategoryName, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .FirstOrDefaultAsync();

            ViewBag.TopCategoryName = mostPopularCategory?.CategoryName;
            ViewBag.TopCategoryOrderCount = mostPopularCategory?.OrderCount ?? 0;

            // Stoğu en az ürün
            var lowestStock = await _context.Products
                .OrderBy(x => x.StockQuantity)
                .Select(x => new { x.ProductName, x.StockQuantity })
                .FirstOrDefaultAsync();

            ViewBag.LowestStockName = lowestStock?.ProductName;
            ViewBag.LowestStock = lowestStock?.StockQuantity ?? 0;

            // En çok iptal edilen ürün
            var mostCancelledRow = await _context.Orders
                .Where(x => x.OrderStatus == "İptal Edildi")
                .GroupBy(x => x.ProductId)
                .Select(g => new { ProductId = g.Key, CancelCount = g.Count() })
                .OrderByDescending(x => x.CancelCount)
                .FirstOrDefaultAsync();

            ViewBag.MostCancelledProductName = mostCancelledRow == null ? null : await _context.Products
                .Where(x => x.ProductId == mostCancelledRow.ProductId)
                .Select(x => x.ProductName)
                .FirstOrDefaultAsync();
            ViewBag.MostCancelledCount = mostCancelledRow?.CancelCount ?? 0;

            // ===== MÜŞTERİLER & KONUM =====

            // En sadık 5 müşteri (sipariş sayısı) + birincisi kart için
            var loyalRows = await _context.Orders
                .GroupBy(x => x.CustomerId)
                .Select(g => new { CustomerId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var customerIds = loyalRows.Select(x => x.CustomerId).ToList();
            var customerNames = await _context.Customers
                .Where(x => customerIds.Contains(x.CustomerId))
                .ToDictionaryAsync(x => x.CustomerId, x => x.CustomerName + " " + x.CustomerSurname);

            var top5LoyalCustomers = loyalRows
                .Select(x => new { Id = x.CustomerId, Name = customerNames[x.CustomerId], x.Count })
                .ToList();

            ViewBag.Top5LoyalCustomers = top5LoyalCustomers;
            ViewBag.MostLoyalCustomerName = top5LoyalCustomers.FirstOrDefault()?.Name;
            ViewBag.MostLoyalCustomerOrderCount = top5LoyalCustomers.FirstOrDefault()?.Count ?? 0;

            // En çok harcayan müşteri (toplam tutar)
            var topSpenderRow = await _context.Orders
                .GroupBy(x => x.CustomerId)
                .Select(g => new { CustomerId = g.Key, TotalSpent = g.Sum(x => x.Quantity * x.Product.UnitPrice) })
                .OrderByDescending(x => x.TotalSpent)
                .FirstOrDefaultAsync();

            ViewBag.TopSpenderName = topSpenderRow == null ? null : await _context.Customers
                .Where(x => x.CustomerId == topSpenderRow.CustomerId)
                .Select(x => x.CustomerName + " " + x.CustomerSurname)
                .FirstOrDefaultAsync();
            ViewBag.TopSpenderTotal = topSpenderRow?.TotalSpent ?? 0;

            // En çok sipariş gelen 5 ülke + birincisi
            var topCountries = await _context.Orders
                .GroupBy(x => x.Customer.CustomerCountry)
                .Select(g => new { Country = g.Key, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .Take(5)
                .ToListAsync();

            ViewBag.TopOrderCountry = topCountries;
            ViewBag.TopCountryName = topCountries.FirstOrDefault()?.Country;
            ViewBag.TopCountryCount = topCountries.FirstOrDefault()?.OrderCount ?? 0;

            // En çok sipariş gelen şehir
            var topCity = await _context.Orders
                .GroupBy(x => x.Customer.CustomerCity)
                .Select(g => new { City = g.Key, OrderCount = g.Count() })
                .OrderByDescending(x => x.OrderCount)
                .FirstOrDefaultAsync();

            ViewBag.TopCityName = topCity?.City;
            ViewBag.TopCityOrderCount = topCity?.OrderCount ?? 0;

            // ===== SATIŞ EĞİLİMLERİ =====

            // En çok kullanılan ödeme yöntemi
            var topPaymentMethod = await _context.Orders
                .GroupBy(x => x.PaymentMethod)
                .Select(g => new { PaymentMethod = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            ViewBag.TopPaymentMethodName = topPaymentMethod?.PaymentMethod;
            ViewBag.TopPaymentMethodCount = topPaymentMethod?.Count ?? 0;

            // Bugünkü satışlar (Today = günün başı, 00:00)
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            ViewBag.TodayOrderCount = await _context.Orders
                .CountAsync(x => x.OrderDate >= today && x.OrderDate < tomorrow);
            ViewBag.TodayQuantitySold = await _context.Orders
                .Where(x => x.OrderDate >= today && x.OrderDate < tomorrow)
                .SumAsync(x => x.Quantity);

            // Son sipariş
            var lastOrder = await _context.Orders
                .OrderByDescending(x => x.OrderDate)
                .Select(x => new
                {
                    Customer = x.Customer.CustomerName + " " + x.Customer.CustomerSurname,
                    Product = x.Product.ProductName
                })
                .FirstOrDefaultAsync();

            ViewBag.LastOrderCustomer = lastOrder?.Customer;
            ViewBag.LastOrderProduct = lastOrder?.Product;

            return View();
        }
    }
}