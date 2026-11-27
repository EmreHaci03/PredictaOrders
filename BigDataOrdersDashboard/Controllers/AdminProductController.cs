using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;
using System.Text.Json;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminProductController : Controller
    {
        private readonly BigDataDbContext _context;

        public AdminProductController(BigDataDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> Index(int page=1)
        {
            if (page < 1) page = 1;
            int pageSize = 100;

            var totalCount = await _context.Products.CountAsync();

            var values = await _context.Products.Include(x => x.Category).OrderBy(x => x.ProductId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.AveragePrice = await _context.Products.AverageAsync(x => x.UnitPrice);
            ViewBag.TotalStock = await _context.Products.SumAsync(x => x.StockQuantity);
            ViewBag.CriticalStock = await _context.Products.CountAsync(x => x.StockQuantity < 50);
            ViewBag.TotalCount = totalCount;
            return View(values);
        }

        [HttpGet]
        public async Task<IActionResult> ProductPerformance()
        {
            ViewBag.TotalProductCount = await _context.Products.CountAsync();

            ViewBag.TopSellingProduct = await _context.Orders
                .GroupBy(x => new {x.ProductId,x.Product.ProductName})
                .Select(g => new
                {
                    Product=g.Key.ProductName,
                    Quantity=g.Sum(x=>x.Quantity)
                }).OrderByDescending(x => x.Quantity)
                .FirstOrDefaultAsync();

            ViewBag.TopRevenueProduct = await _context.Orders
                .GroupBy(x => new { x.ProductId, x.Product.ProductName })
                .Select(g => new
                {
                    Product = g.Key.ProductName,
                    TotalRevenue = g.Sum(x => x.Quantity * x.Product.UnitPrice)
                }).OrderByDescending(x => x.TotalRevenue).FirstOrDefaultAsync();

            var unSoldProducts = await _context.Products
                .Where(p => !_context.Orders.Any(o => o.ProductId == p.ProductId))
                .Select(x => new { Id = x.ProductId, Product = x.ProductName })
                .ToListAsync();

            ViewBag.UnSoldProducts= unSoldProducts;
            ViewBag.UnSoldProductCount = unSoldProducts?.Count ?? 0; 

            ViewBag.MostCancelledProduct=await _context.Orders
                .Where(x=>x.OrderStatus=="İptal Edildi")
                .GroupBy(x=>new { x.ProductId, x.Product.ProductName})
                .Select(g => new
                {
                    Product=g.Key.ProductName,  
                    CancelCount=g.Count()
                }).OrderByDescending(x=>x.CancelCount).FirstOrDefaultAsync();


            ViewBag.CriticalStockCount = await _context.Products
                .Where(x=>x.StockQuantity<50)
                .CountAsync();

            ViewBag.CriticalStockList = await _context.Products
                .Where(x => x.StockQuantity < 50)
                .OrderByDescending(x => x.StockQuantity)
                .Take(10)
                .Select(g => new
                {
                    g.ProductName,
                    g.StockQuantity,
                    g.Category.CategoryName,
                }).ToListAsync();

            ViewBag.AvgProductPrice = await _context.Products.AverageAsync(x => x.UnitPrice);

            ViewBag.TopOriginCountry = await _context.Products
                .GroupBy(x => x.CountryOfOrigin)
                .Select(g => new
                {
                    Country=g.Key,
                    Count = g.Count()
                }).OrderByDescending(x => x.Count).FirstOrDefaultAsync();


            var categoryRevenue = await _context.Orders
                .GroupBy(x=>new {x.Product.CategoryId,x.Product.Category.CategoryName})
                .Select(g => new
                {
                    Category=g.Key.CategoryName,
                    Revenue=g.Sum(x=>x.Quantity*x.Product.UnitPrice)
                }).OrderByDescending(x=>x.Revenue).ToListAsync();

            ViewBag.CategoryRevenueCategory = JsonSerializer.Serialize(categoryRevenue.Select(x => x.Category));
            ViewBag.CategoryRevenue = JsonSerializer.Serialize(categoryRevenue.Select(x => x.Revenue));


            var TopProductsChart = await _context.Orders
                .GroupBy(x => new { x.ProductId, x.Product.ProductName })
                .Select(g => new
                {
                    Product = g.Key.ProductName,
                    Quantity = g.Sum(x=>x.Quantity)
                }).OrderByDescending(x => x.Quantity).Take(10).ToListAsync();

            ViewBag.TopProducts=JsonSerializer.Serialize(TopProductsChart.Select(x=>x.Product));
            ViewBag.TopProductsRevenue = JsonSerializer.Serialize(TopProductsChart.Select(x => x.Quantity));


            var cancelRates = await _context.Orders
    .GroupBy(x => new { x.Product.CategoryId, x.Product.Category.CategoryName })
    .Select(g => new
    {
        Category = g.Key.CategoryName,
        Total = g.Count(),
        Cancelled = g.Count(x => x.OrderStatus == "İptal Edildi")
    })
    .ToListAsync();


            var cancelChart = cancelRates
                .Select(x => new
                {
                    x.Category,
                    Rate = x.Total > 0 ? Math.Round(x.Cancelled * 100.0 / x.Total, 1) : 0
                })
                .OrderByDescending(x => x.Rate)
                .ToList();

            ViewBag.CancelCategories = JsonSerializer.Serialize(cancelChart.Select(x => x.Category));
            ViewBag.CancelRates = JsonSerializer.Serialize(cancelChart.Select(x => x.Rate));


            var stock = await _context.Products
                .GroupBy(x => 1)
                .Select(g => new
                {
                    Value = g.Sum(x => x.StockQuantity * x.UnitPrice),

                    Quantity = g.Sum(x => x.StockQuantity),

                }).FirstOrDefaultAsync();

            ViewBag.TotalStockValue = stock?.Value ?? 0;
            ViewBag.TotalStockQuantity = stock?.Quantity ?? 0;


            var categoryProducts = await _context.Products
                .GroupBy(x => new { x.CategoryId, x.Category.CategoryName })
                .Select(g => new
                {
                    CategoryName = g.Key.CategoryName,
                    Count = g.Count()
                }).OrderByDescending(x => x.Count).ToListAsync();

            ViewBag.CategoryProductNames = JsonSerializer.Serialize(categoryProducts.Select(x => x.CategoryName));
            ViewBag.CategoryProductCounts = JsonSerializer.Serialize(categoryProducts.Select(x => x.Count));


            var totalRevenue = await _context.Orders.SumAsync(x => x.Quantity * x.Product.UnitPrice);
            var TopRevenueProducts = await _context.Orders
                .GroupBy(x => new { x.ProductId, x.Product.ProductName, x.Product.Category.CategoryName })
                .Select(g => new
                {
                    Product = g.Key.ProductName,
                    Category = g.Key.CategoryName,
                    Quantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Quantity * x.Product.UnitPrice)
                }).OrderByDescending(x => x.Revenue)
                .Take(5).ToListAsync();


            ViewBag.TopRevenueProducts = TopRevenueProducts.Select(x => new
            {
                x.Product,
                x.Category,
                x.Quantity,
                x.Revenue,
                Share = totalRevenue > 0 ? (double)(x.Revenue * 100 / totalRevenue) : 0
            });


            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CreateProduct()
        {
            ViewBag.Categories = await _context.Categories
                .Select(x => new SelectListItem
                {
                    Text = x.CategoryName,
                    Value = x.CategoryId.ToString()
                }).ToListAsync();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

      [HttpPost]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var values = await _context.Products.FindAsync(id);
            if (values == null)
                return NotFound();
            _context.Products.Remove(values);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateProduct(int id)
        {
            var values=await _context.Products.FindAsync(id);
            if (values == null)
                return RedirectToAction(nameof(Index));

            ViewBag.Categories = await _context.Categories.Select(x => new SelectListItem
            {
                Text=x.CategoryName,
                Value=x.CategoryId.ToString()
            }).ToListAsync();

            return View(values);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProduct(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}
