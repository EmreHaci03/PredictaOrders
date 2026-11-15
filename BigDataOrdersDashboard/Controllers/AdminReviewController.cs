using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminReviewController : Controller
    {
        private readonly BigDataDbContext _context;

        public AdminReviewController(BigDataDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public async Task<IActionResult> Index(int page=1)
        {
            int pageSize = 100;

            var totalCount=await _context.Reviews.CountAsync();
            var values = await _context.Reviews.Include(x=>x.Customer).Include(x=>x.Product).OrderByDescending(x => x.ReviewDate).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();

            ViewBag.PageSize = pageSize;
            ViewBag.CurrentPage=page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            ViewBag.TotalCount=totalCount;
            ViewBag.AverageRating = await _context.Reviews.AverageAsync(x => (double)x.Rating);
            ViewBag.PositiveCount = await _context.Reviews.CountAsync(x => x.Sentiment == "Olumlu");
            ViewBag.NegativeCount = await _context.Reviews.CountAsync(x => x.Sentiment == "Olumsuz");
            return View(values);
        }
        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var value = await _context.Reviews.FindAsync(id);
            if(value == null)
                return NotFound();
            _context.Reviews.Remove(value);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
