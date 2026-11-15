using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BigDataOrdersDashboard.ViewComponents.AdminDashboard
{
    public class DashboardReviewInsightsViewComponent : ViewComponent
    {
        private readonly BigDataDbContext _context;

        public DashboardReviewInsightsViewComponent(BigDataDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // ===================== 1) PUAN DAĞILIMI =====================

            var totalReviewCount = await _context.Reviews.CountAsync();
            ViewBag.TotalReviewCount = totalReviewCount;

            // Tablo boşsa AverageAsync hata verir
            ViewBag.AverageRating = totalReviewCount > 0
                ? await _context.Reviews.AverageAsync(x => (double)x.Rating)
                : 0;

            // Her puandan kaç yorum var (5 → 1)
            ViewBag.RatingCountList = await _context.Reviews
                .GroupBy(x => x.Rating)
                .Select(g => new
                {
                    Rating = (int)g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Rating)
                .ToListAsync();

            // Duygu dağılımı
            var sentimentCounts = await _context.Reviews
                .GroupBy(x => x.Sentiment)
                .Select(g => new
                {
                    Sentiment = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            double Rate(string sentiment)
            {
                var count = sentimentCounts.FirstOrDefault(x => x.Sentiment == sentiment)?.Count ?? 0;
                return totalReviewCount > 0 ? count * 100.0 / totalReviewCount : 0;
            }

            ViewBag.PositiveRate = Rate("Olumlu");
            ViewBag.NeutralRate = Rate("Nötr");
            ViewBag.NegativeRate = Rate("Olumsuz");

            // ===================== 2) ÜRÜN PUANLARI =====================

            // En az 20 yorumu olan ürünlerin ortalaması
            // (tek bir 5★ yorumu olan ürün birinci çıkmasın diye)
            int minReviewCount = 20;

            var productRatings = await _context.Reviews
                .GroupBy(x => x.ProductId)
                .Where(g => g.Count() >= minReviewCount)            // SQL'de HAVING
                .Select(g => new
                {
                    ProductId = g.Key,
                    AverageRating = g.Average(x => (double)x.Rating),
                    ReviewCount = g.Count()
                })
                .ToListAsync();

            // Ürün sayısı az olduğu için sıralamayı hafızada yapıyoruz
            var topRatedRows = productRatings
                .OrderByDescending(x => x.AverageRating).ThenByDescending(x => x.ReviewCount)
                .Take(3).ToList();

            var lowestRatedRows = productRatings
                .OrderBy(x => x.AverageRating).ThenByDescending(x => x.ReviewCount)
                .Take(3).ToList();

            // Sadece gereken 6 ürünün adını çek
            var productIds = topRatedRows.Concat(lowestRatedRows).Select(x => x.ProductId).Distinct().ToList();
            var productNames = await _context.Products
                .Where(x => productIds.Contains(x.ProductId))
                .ToDictionaryAsync(x => x.ProductId, x => x.ProductName);

            ViewBag.MinReviewCount = minReviewCount;

            ViewBag.TopRatedProducts = topRatedRows.Select(x => new
            {
                Name = productNames[x.ProductId],
                x.AverageRating,
                x.ReviewCount
            }).ToList();

            ViewBag.LowestRatedProducts = lowestRatedRows.Select(x => new
            {
                Name = productNames[x.ProductId],
                x.AverageRating,
                x.ReviewCount
            }).ToList();

            // ===================== 3) ÖNE ÇIKAN YORUMLAR =====================

            // En iyi yorum: en yüksek puanlılar içinde en güncel olanı
            ViewBag.BestReview = await _context.Reviews
                .OrderByDescending(x => x.Rating)
                .ThenByDescending(x => x.ReviewDate)
                .Select(x => new
                {
                    Text = x.ReviewText,
                    Rating = (int)x.Rating,
                    Customer = x.Customer.CustomerName + " " + x.Customer.CustomerSurname,
                    Product = x.Product.ProductName
                })
                .FirstOrDefaultAsync();

            // En kötü yorum: en düşük puanlılar içinde en güncel olanı
            ViewBag.WorstReview = await _context.Reviews
                .OrderBy(x => x.Rating)
                .ThenByDescending(x => x.ReviewDate)
                .Select(x => new
                {
                    Text = x.ReviewText,
                    Rating = (int)x.Rating,
                    Customer = x.Customer.CustomerName + " " + x.Customer.CustomerSurname,
                    Product = x.Product.ProductName
                })
                .FirstOrDefaultAsync();

            return View();
        }
    }
}