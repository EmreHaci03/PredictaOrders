using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.DTOS;
using BigDataOrdersDashboard.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using System.Diagnostics.Metrics;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminCustomerSegmentationController : Controller
    {
        private readonly BigDataDbContext _context;
        private readonly MLContext _mlContext;

        public AdminCustomerSegmentationController(BigDataDbContext context, MLContext mlContext)
        {
            _context = context;
            _mlContext = mlContext;
        }

        public async Task<ActionResult> CityCustomerLoyalty(string Country)
        {
            var referanceDate = new DateTime(2026, 1, 1);

            ViewBag.Country = Country;
            ViewBag.Countries = await _context.Customers
                .Where(x => x.CustomerCountry != null && x.CustomerCountry != "")
                .Select(x => x.CustomerCountry)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            if (string.IsNullOrEmpty(Country))
                return View(new List<LoyaltyScoreDto>());

            var customers = await _context.Orders
                .Where(x => x.Customer.CustomerCountry == Country && x.OrderDate < referanceDate)
                .GroupBy(x => new { x.Customer.CustomerId, x.Customer.CustomerName, x.Customer.CustomerSurname })
                .Select(g => new LoyaltyScoreDto
                {
                    CustomerName = g.Key.CustomerName + " " + g.Key.CustomerSurname,
                    LastOrderDate = g.Max(x => x.OrderDate),
                    TotalSpend = g.Sum(x => x.Quantity * x.Product.UnitPrice),
                    OrderCount = g.Count(),
                    DaysSinceLastOrder = EF.Functions.DateDiffDay(g.Max(x => x.OrderDate), referanceDate)
                }).ToListAsync();

            if (customers.Count == 0)
                return View(new List<LoyaltyScoreDto>());


            int n = customers.Count;

            //Recency 'de Müşterinin Son Sipariş Tarihinden Bugüne geçen tarihe kadar olan kısmı kapsar.
            var rScores = customers
                .OrderByDescending(x => x.DaysSinceLastOrder)
                .Select((x, i) => new { Customer = x, Score = i * 5 / n + 1 })
                .ToDictionary(x => x.Customer, x => x.Score);

            //Freguency Müşterinin Yaptığı Alışveriş Sıklığına Göre Bulunan Bir Değerdir.
            var fScores = customers
                .OrderBy(x => x.OrderCount)
                .Select((x, i) => new { Customer = x, Score = i * 5 / n + 1 })
                .ToDictionary(x => x.Customer, x => x.Score);

            //Monetary  Müşterinin Belirtilen Zaman Aralığında Yaptığı Toplam Harcamadır.
            var mScores = customers
                .OrderBy(x => x.TotalSpend)
                .Select((x, i) => new { Customer = x, Score = i * 5 / n + 1 })
                .ToDictionary(x => x.Customer, x => x.Score);

            foreach (var c in customers)
            {
                int r = rScores[c];
                int f = fScores[c];
                int m = mScores[c];

                int total = r + f + m;
                c.LoyaltyScore = total * 100 / 15;

                c.LoyaltyLevel = c.LoyaltyScore switch
                {
                    >= 86 => "Çok Sadık",
                    >= 66 => "Sadık",
                    >= 46 => "Kararsız",
                    _ => "Kaybolmak Üzere"
                };
            }

            var result = customers
                .OrderByDescending(x => x.LoyaltyScore)
                .ThenByDescending(x => x.OrderCount)
                .ToList();

            return View(result);
        }

        public async Task<ActionResult> CityCustomerLoyaltyMl(string country)
        {
            var referenceDate = new DateTime(2026, 1, 1);

            // ===== ADIM 1: Ülke listesi (dropdown için) =====
            ViewBag.Country = country;
            ViewBag.Countries = await _context.Customers
                .Where(x => x.CustomerCountry != null && x.CustomerCountry != "")
                .Select(x => x.CustomerCountry)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            if (string.IsNullOrEmpty(country))
                return View("CityCustomerLoyaltyMl", new List<LoyaltyScoreDto>());

            // ===== ADIM 2: Müşterilerin ham verisi =====
            var customers = await _context.Orders
                .Where(x => x.Customer.CustomerCountry == country && x.OrderDate < referenceDate)
                .GroupBy(x => new { x.Customer.CustomerId, x.Customer.CustomerName, x.Customer.CustomerSurname })
                .Select(g => new LoyaltyScoreDto
                {
                    CustomerName = g.Key.CustomerName + " " + g.Key.CustomerSurname,
                    LastOrderDate = g.Max(x => x.OrderDate),
                    TotalSpend = g.Sum(x => x.Quantity * x.Product.UnitPrice),
                    OrderCount = g.Count(),
                    DaysSinceLastOrder = EF.Functions.DateDiffDay(g.Max(x => x.OrderDate), referenceDate)
                }).ToListAsync();

            // 4 gruba ayıracağımız için en az 4 müşteri lazım
            if (customers.Count < 4)
                return View("CityCustomerLoyaltyMl", new List<LoyaltyScoreDto>());

            // ===== ADIM 3: Veriyi modelin anlayacağı listeye çevir =====
            var inputs = new List<LoyaltyInput>();

            foreach (var c in customers)
            {
                inputs.Add(new LoyaltyInput
                {
                    Recency = c.DaysSinceLastOrder,
                    Frequency = c.OrderCount,
                    Monetary = (float)c.TotalSpend
                });
            }

            // C# listesini ML.NET tablosuna çevir
            var dataView = _mlContext.Data.LoadFromEnumerable(inputs);

            // ===== ADIM 4: Pipeline (modelin yapacağı işlemler sırası) =====
            var pipeline = _mlContext.Transforms
                .Concatenate("Features", "Recency", "Frequency", "Monetary")             // 3 kolonu birleştir
                .Append(_mlContext.Transforms.NormalizeMinMax("Features"))                // 0-1 arasına çek
                .Append(_mlContext.Clustering.Trainers.KMeans("Features", numberOfClusters: 4)); // 4 gruba ayır

            // ===== ADIM 5: Modeli eğit ve sonuçları al =====
            var model = pipeline.Fit(dataView);                  // eğit
            var transformed = model.Transform(dataView);         // her müşteriye grup ver

            var predictions = _mlContext.Data
                .CreateEnumerable<LoyaltyPrediction>(transformed, reuseRowObject: false)
                .ToList();                                       // ML.NET tablosunu tekrar C# listesine çevir

            // ===== ADIM 6: Her müşteriye 0-100 skor ver =====
            int minDays = customers.Min(x => x.DaysSinceLastOrder);
            int maxDays = customers.Max(x => x.DaysSinceLastOrder);

            int minOrder = customers.Min(x => x.OrderCount);
            int maxOrder = customers.Max(x => x.OrderCount);

            decimal minSpend = customers.Min(x => x.TotalSpend);
            decimal maxSpend = customers.Max(x => x.TotalSpend);

            foreach (var c in customers)
            {
                // Recency: az gün = iyi, o yüzden 1'den çıkarıyoruz
                double r = 1 - (double)(c.DaysSinceLastOrder - minDays) / (maxDays - minDays);

                // Frequency: çok sipariş = iyi
                double f = (double)(c.OrderCount - minOrder) / (maxOrder - minOrder);

                // Monetary: çok harcama = iyi
                double m = (double)((c.TotalSpend - minSpend) / (maxSpend - minSpend));

                // Üçünün ortalaması × 100
                c.LoyaltyScore = (int)Math.Round((r + f + m) / 3 * 100);
            }

            // ===== ADIM 7: Her grubun skorlarını topla =====
            var clusterScores = new Dictionary<uint, List<int>>();

            for (int i = 0; i < customers.Count; i++)
            {
                uint clusterId = predictions[i].ClusterId;

                if (!clusterScores.ContainsKey(clusterId))
                    clusterScores[clusterId] = new List<int>();

                clusterScores[clusterId].Add(customers[i].LoyaltyScore);
            }

            // ===== ADIM 8: Grupları ortalama skora göre sırala ve isim ver =====
            var sortedClusters = clusterScores
                .OrderByDescending(x => x.Value.Average())
                .Select(x => x.Key)
                .ToList();

            var levels = new List<string> { "Çok Sadık", "Sadık", "Kararsız", "Kaybolmak Üzere" };
            var clusterNames = new Dictionary<uint, string>();

            for (int i = 0; i < sortedClusters.Count; i++)
            {
                clusterNames[sortedClusters[i]] = levels[i];
            }

            // ===== ADIM 9: Her müşteriye grubunun adını ver =====
            for (int i = 0; i < customers.Count; i++)
            {
                uint clusterId = predictions[i].ClusterId;
                customers[i].LoyaltyLevel = clusterNames[clusterId];
            }

            // ===== ADIM 10: Sırala ve gönder =====
            var result = customers
                .OrderByDescending(x => x.LoyaltyScore)
                .ThenByDescending(x => x.OrderCount)
                .ToList();

            return View("CityCustomerLoyaltyMl", result);                    
        }

        public async Task<IActionResult> CustomerRetention(int page=1)
        {
            var referanceDate = new DateTime(2026, 1, 1);
            var start2024 = new DateTime(2024, 1, 1);
            var start2025 = new DateTime(2025, 1, 1);
            var churnLimit = referanceDate.AddDays(-180);

            int pageSize = 20;
            if (page < 1)
                page = 1;


            var customers2024 = await _context.Orders
                .Where(x => x.OrderDate >= start2024 && x.OrderDate < start2025)
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync();


            var retainedCustomers = await _context.Orders
                .Where(x => x.OrderDate >= start2024 && x.OrderDate < referanceDate)
                .GroupBy(x => x.CustomerId)
                .Where(g => g.Min(x => x.OrderDate) < start2025 && g.Max(x => x.OrderDate) >= start2025)
                .CountAsync();

                double retentionRate = customers2024 > 0
                ? retainedCustomers * 100.0 / customers2024
                : 0;

            var totalCustomers = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .Select(x => x.CustomerId)
                .Distinct()
                .CountAsync();


            var churnedCustomers = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .GroupBy(x => x.CustomerId)
                .Where(g => g.Max(x => x.OrderDate) < churnLimit)
                .CountAsync();


            double churnRate = totalCustomers > 0
                ? churnedCustomers * 100.0 / totalCustomers
                : 0;





            var churnedList = await _context.Orders
                .Where(x => x.OrderDate < referanceDate)
                .GroupBy(x => new { x.CustomerId, x.Customer.CustomerName, x.Customer.CustomerSurname, x.Customer.CustomerCountry })
                .Where(x => x.Max(x => x.OrderDate) < churnLimit)
                .Select(g => new
                {
                    NameSurname = g.Key.CustomerName + " " + g.Key.CustomerSurname,
                    OrderCount = g.Count(),
                    Country = g.Key.CustomerCountry,
                    DaysAway = EF.Functions.DateDiffDay(g.Max(x => x.OrderDate), referanceDate)
                }).OrderByDescending(x => x.DaysAway)
                .Skip((page-1)*pageSize)
                .Take(pageSize)
                .ToListAsync();



            ViewBag.Customers2024 = customers2024;
            ViewBag.RetainedCustomers = retainedCustomers;
            ViewBag.RetentionRate = retentionRate;

            ViewBag.Currentpage = page;
            ViewBag.TotalChurnedCustomerCount = churnedCustomers;
            ViewBag.TotalPages = (int)Math.Ceiling(churnedCustomers / (double)pageSize);

            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.ChurnedCustomers = churnedCustomers;
            ViewBag.ChurnRate = churnRate;

            ViewBag.ChurnedList = churnedList;



            return View();
        }



    }
}