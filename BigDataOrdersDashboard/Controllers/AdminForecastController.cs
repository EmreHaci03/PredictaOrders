using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
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
                .Select(g => new { City = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .Select(x => x.City)
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


        public class MonthlyInput
        {
            public float OrderCount { get; set; }
        }

        public class MonthlyOutput
        {
            public float[] Forecast { get; set; } = Array.Empty<float>();
        }
    }
}