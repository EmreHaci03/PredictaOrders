using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Printing;

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
