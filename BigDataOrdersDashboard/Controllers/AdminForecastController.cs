using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using Newtonsoft.Json.Linq;
using System.Globalization;
using System.Text.Json;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminForecastController : Controller
    {
        private readonly MLContext _mLContext;
        private readonly BigDataDbContext _context;

        public AdminForecastController(MLContext mLContext, BigDataDbContext context)
        {
            _mLContext = mLContext;
            _context = context;
        }

        public async Task<IActionResult> PaymentMethodForecast()
        {
            var horizon = 12;
            var tr = new CultureInfo("tr-TR");

            var start = new DateTime(2023, 1, 1);
            var end = new DateTime(2026, 1, 1);      
            var lastDataMonth = end.AddMonths(-1);    

            var raw = await _context.Orders
                .Where(x => x.OrderDate >= start && x.OrderDate < end)
                .GroupBy(x => new { x.OrderDate.Year, x.OrderDate.Month, x.PaymentMethod })
                .Select(g => new
                {
                    g.Key.PaymentMethod,
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .ToListAsync();

            if (!raw.Any())
                return View();

            var forecastMonths = Enumerable.Range(1, horizon)
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();

            var series = new List<object>();

            foreach (var method in raw.Select(x => x.PaymentMethod).Distinct())
            {
                var history = new List<MonthlyInput>();

                for (var m = start; m < end; m = m.AddMonths(1))
                {
                    var count = raw.FirstOrDefault(x =>
                        x.Year == m.Year && x.Month == m.Month && x.PaymentMethod == method)?.Count ?? 0;

                    history.Add(new MonthlyInput { OrderCount = count });
                }

                int windowSize = history.Count >= 36 ? 12 : 3;

                var pipeline = _mLContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(MonthlyOutput.Forecast),
                    inputColumnName: nameof(MonthlyInput.OrderCount),
                    windowSize: windowSize,
                    seriesLength: Math.Min(history.Count, windowSize * 2),
                    trainSize: history.Count,
                    horizon: horizon);

                var dataView = _mLContext.Data.LoadFromEnumerable(history);
                var model = pipeline.Fit(dataView);
                var engine = model.CreateTimeSeriesEngine<MonthlyInput, MonthlyOutput>(_mLContext);
                var prediction = engine.Predict();

                series.Add(new
                {
                    method = method,
                    values = prediction.Forecast.Select(v => Math.Round(Math.Max(0f, v)))
                });
            }

            ViewBag.MonthLabels = JsonSerializer.Serialize(forecastMonths.Select(m => m.ToString("MMM yy", tr)));
            ViewBag.PaymentSeries = JsonSerializer.Serialize(series);

            return View();
        }
        public async Task<IActionResult> CityForecast(string country)
        {
            var horizon = 12;
            var tr = new CultureInfo("tr-TR");

            // Dropdown için ülke listesi (her durumda dolu olmalı)
            ViewBag.Country = country;
            ViewBag.Countries = await _context.Customers
                .Where(x => x.CustomerCountry != null && x.CustomerCountry != "")
                .Select(x => x.CustomerCountry)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            if (string.IsNullOrEmpty(country))
                return View();

            var start = new DateTime(2023, 1, 1);
            var end = new DateTime(2026, 1, 1);
            var lastDataMonth = end.AddMonths(-1);
            var topCities = await _context.Orders
                .Where(x => x.OrderDate >= start && x.OrderDate < end
                         && x.Customer.CustomerCountry == country
                         && x.Customer.CustomerCity != null)
                .GroupBy(x => x.Customer.CustomerCity)
                .OrderByDescending(x => x.Count())
                .Take(6)
                .Select(x =>x.Key)
                .ToListAsync();

            if (!topCities.Any())
                return View();

            var raw = await _context.Orders
                .Where(x => x.OrderDate >= start && x.OrderDate < end
                         && x.Customer.CustomerCountry == country
                         && topCities.Contains(x.Customer.CustomerCity))
                .GroupBy(x => new { x.Customer.CustomerCity, x.OrderDate.Year, x.OrderDate.Month })
                .Select(g => new
                {
                    City = g.Key.CustomerCity,
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                })
                .ToListAsync();

            if (!raw.Any())
                return View();

            var forecastMonths = Enumerable.Range(1, horizon)
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();

            var series = new List<object>();

            foreach (var city in topCities)
            {
                var history = new List<MonthlyInput>();

                for (var m = start; m < end; m = m.AddMonths(1))
                {
                    var count = raw.FirstOrDefault(x =>
                        x.City == city && x.Year == m.Year && x.Month == m.Month)?.Count ?? 0;

                    history.Add(new MonthlyInput { OrderCount = count });
                }

                int windowSize = history.Count >= 36 ? 12 : 3;

                var pipeline = _mLContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(MonthlyOutput.Forecast),
                    inputColumnName: nameof(MonthlyInput.OrderCount),
                    windowSize: windowSize,
                    seriesLength: Math.Min(history.Count, windowSize * 2),
                    trainSize: history.Count,
                    horizon: horizon);

                var dataView = _mLContext.Data.LoadFromEnumerable(history);
                var model = pipeline.Fit(dataView);
                var engine = model.CreateTimeSeriesEngine<MonthlyInput, MonthlyOutput>(_mLContext);
                var prediction = engine.Predict();

                series.Add(new
                {
                    method = city,
                    values = prediction.Forecast.Select(v => Math.Round(Math.Max(0f, v)))
                });
            }

            ViewBag.MonthLabels = JsonSerializer.Serialize(forecastMonths.Select(x => x.ToString("MMM yy", tr)));
            ViewBag.Cities = JsonSerializer.Serialize(series);

            return View();
        }

        public async Task<IActionResult> CountryForecast(string Country)
        {
            var horizon = 12;
            var tr = new CultureInfo("tr-TR");
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2026, 1, 1);
            var lastDataMonth = endDate.AddMonths(-1);

            ViewBag.CountryList = await _context.Customers
                .Select(x=>x.CustomerCountry)
                .Distinct()
                .OrderBy(x=>x)
                .ToListAsync();

            if (string.IsNullOrEmpty(Country))
                return View();

            ViewBag.SelectedCountry = Country;

            var topCountries=await _context.Orders
                .Where(x=>x.OrderDate>=startDate && x.OrderDate<endDate && x.Customer.CustomerCountry!=null && x.Customer.CustomerCountry!=" " && x.Customer.CustomerCountry==Country)
                .GroupBy(x => x.Customer.CustomerCountry)
                .OrderByDescending(x => x.Count())
                .Take(6)
                .Select(x => x.Key)
                .ToListAsync();


            var raw = await _context.Orders
                .Where(x => x.OrderDate >= startDate && x.OrderDate < endDate && topCountries.Contains(x.Customer.CustomerCountry))
                .GroupBy(x => new { x.Customer.CustomerCountry,x.OrderDate.Year,x.OrderDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Country=g.Key.CustomerCountry,
                    Count = g.Count()
                }).ToListAsync();

            if (!raw.Any())
                return View();


            var forecastMonths = Enumerable.Range(1, horizon)
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();


            var series = new List<object>();

            foreach(var country in topCountries)
            {
                var history = new List<MonthlyInput>();

                for(var c = startDate; c < endDate; c = c.AddMonths(1))
                {
                    var count=raw.FirstOrDefault(x=>x.Year==c.Year && x.Month==c.Month && x.Country==country)?.Count ?? 0;

                    history.Add(new MonthlyInput { OrderCount=count });
                }

                ViewBag.HistoryTotal = history.Sum(x => x.OrderCount);
                ViewBag.LastYearTotal=history.TakeLast(12).Sum(x => x.OrderCount);


                int windowSize = history.Count >= 36 ? 12 : 3;
                var pipeline = _mLContext.Forecasting.ForecastBySsa(
                    inputColumnName: nameof(MonthlyInput.OrderCount),
                    outputColumnName: nameof(MonthlyOutput.Forecast),
                    seriesLength: Math.Min(history.Count, windowSize * 2),
                    trainSize: history.Count,
                    horizon: horizon,
                    windowSize: windowSize
                    );

                var dataView = _mLContext.Data.LoadFromEnumerable(history);
                var model=pipeline.Fit(dataView);
                var engine = model.CreateTimeSeriesEngine<MonthlyInput, MonthlyOutput>(_mLContext);
                var prediction = engine.Predict();


                series.Add(new
                {
                    method = country,
                    values = prediction.Forecast.Select(v => Math.Round(Math.Max(0f, v)))
                });

            }
            ViewBag.MonthLabels = JsonSerializer.Serialize(forecastMonths.Select(x => x.ToString("MMM yy", tr)));
            ViewBag.Countries = JsonSerializer.Serialize(series);


            return View();
        }
        public async Task<IActionResult> CategoryForecast(string Category)
        {

            var horizon = 12;
            var tr = new CultureInfo("tr-TR");

            ViewBag.AllCategories = await _context.Categories
                .Select(x=>x.CategoryName)
                .OrderBy(x=>x)
                .ToListAsync();

            if (string.IsNullOrWhiteSpace(Category))
                return View();


            ViewBag.SelectedCategory = Category;


            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2026, 1, 1);
            var lastDataMonth = endDate.AddMonths(-1);

            var raw = await _context.Orders
                .Where(x => x.OrderDate >= startDate && x.OrderDate < endDate && x.Product.Category.CategoryName==Category)
                .GroupBy(x => new { x.Product.CategoryId, x.Product.Category.CategoryName,x.OrderDate.Year,x.OrderDate.Month})
                .Select(g => new
                {
                    Category = g.Key.CategoryName,
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                }).ToListAsync();

            if(!raw.Any())
                return View();

            var forecastMonths = Enumerable.Range(1, horizon) // 1 den 12 ye tam sayı dizisi oluşturur.
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();


            var series = new List<object>();


            foreach(var category in raw.Select(x => x.Category).Distinct())
            {
                var history = new List<MonthlyInput>();

                for(var c = startDate; c < endDate; c = c.AddMonths(1))
                {
                    var count=raw.FirstOrDefault(x=>x.Year==c.Year && x.Month==c.Month && x.Category==category)?.Count ?? 0;

                    history.Add(new MonthlyInput { OrderCount = count });
                }

                ViewBag.HistoryTotal = history.Sum(x => x.OrderCount);
                ViewBag.LastYearTotal = history.TakeLast(12).Sum(x => x.OrderCount);


                int windowSize = history.Count >= 36 ? 12 : 3;

                var pipeline = _mLContext.Forecasting.ForecastBySsa(
                    inputColumnName: nameof(MonthlyInput.OrderCount),
                    outputColumnName: nameof(MonthlyOutput.Forecast),
                    windowSize: windowSize,
                    seriesLength: Math.Min(history.Count, windowSize * 2),
                    trainSize: history.Count,
                    horizon: horizon
                    );

                var dataView = _mLContext.Data.LoadFromEnumerable(history);
                var model = pipeline.Fit(dataView);
                var engine = model.CreateTimeSeriesEngine<MonthlyInput, MonthlyOutput>(_mLContext);
                var prediction = engine.Predict();



                series.Add(new
                {
                    method = category,
                    values = prediction.Forecast.Select(v => Math.Round(Math.Max(0f, v)))
                });
            }
            ViewBag.CategoryLabels = JsonSerializer.Serialize(forecastMonths.Select(x => x.ToString("MMM yy", tr)));
            ViewBag.Categories = JsonSerializer.Serialize(series);

            return View();

        }

        public async Task<IActionResult> SalesForecast()
        {
            var horizon = 12;
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2026, 1, 1);
            var lastDataMonth = endDate.AddMonths(-1);
            var tr =new CultureInfo("tr-TR");


            var raw = await _context.Orders
                .Where(x => x.OrderDate >= startDate && x.OrderDate < endDate)
                .GroupBy(x => new { x.OrderDate.Year, x.OrderDate.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count(),
                    Revenue = g.Sum(x => x.Quantity * x.Product.UnitPrice)
                }).ToListAsync();

            if (!raw.Any())
                return View();


            var series = new List<object>();

            var orderHistory = new List<MonthlyInput>();
            var revenueHistory = new List<MonthlyInput>();

            for (var o = startDate; o < endDate; o = o.AddMonths(1))
            {

                var month = raw.FirstOrDefault(x => x.Year == o.Year && x.Month == o.Month);

                orderHistory.Add(new MonthlyInput { OrderCount = month?.Count ?? 0 });
                revenueHistory.Add(new MonthlyInput { OrderCount =(float)(month?.Revenue ?? 0 )});

            }


            var orderForecast = Forecast(orderHistory, horizon);
            var revenueForecast = Forecast(revenueHistory, horizon);


            var forecastMonths = Enumerable.Range(1, horizon)
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();

            ViewBag.MonthLabels = JsonSerializer.Serialize(forecastMonths.Select(x => x.ToString("MMM yy", tr)));
            ViewBag.OrderForecast = JsonSerializer.Serialize(orderForecast);
            ViewBag.RevenueForecast = JsonSerializer.Serialize(revenueForecast);

            ViewBag.LastYearOrders = JsonSerializer.Serialize(orderHistory.TakeLast(12).Select(x => x.OrderCount));
            ViewBag.LastYearRevenue = JsonSerializer.Serialize(revenueHistory.TakeLast(12).Select(x => Math.Round(x.OrderCount)));
            return View();

        }

        private List<double> Forecast(List<MonthlyInput> history, int horizon)
        {
                int windowSize = history.Count >= 36 ? 12 : 3;

                var pipeline = _mLContext.Forecasting.ForecastBySsa(

                    inputColumnName: nameof(MonthlyInput.OrderCount),
                    outputColumnName: nameof(MonthlyOutput.Forecast),
                    trainSize: history.Count,
                    seriesLength: Math.Min(history.Count, windowSize * 2),
                    windowSize: windowSize,
                    horizon: horizon
                    );

                var dataView = _mLContext.Data.LoadFromEnumerable(history);
                var model = pipeline.Fit(dataView);
                var engine = model.CreateTimeSeriesEngine<MonthlyInput, MonthlyOutput>(_mLContext);
                var prediction = engine.Predict();

                return prediction.Forecast.Select(v => Math.Round((double)Math.Max(0f, v))).ToList();
            }
        }

        public class MonthlyInput
        {
            public float OrderCount { get; set; }
        }

        public class MonthlyOutput
        {
            public float[] Forecast { get; set; } = Array.Empty<float>();
        }
    
}