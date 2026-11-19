using BigDataOrdersDashboard.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using System.Globalization;
using System.Text.Json;

namespace BigDataOrdersDashboard.Controllers
{
    public class ForecastController : Controller
    {
        private readonly MLContext _mLContext;
        private readonly BigDataDbContext _context;

        public ForecastController(MLContext mLContext, BigDataDbContext context)
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
                .Where(x => x.OrderDate > start && x.OrderDate <= end)
                .GroupBy(x => new { x.OrderDate.Year, x.OrderDate.Month , x.PaymentMethod })
                .Select(g => new
                {
                    g.Key.PaymentMethod,
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                }).ToListAsync();

            if (!raw.Any())
                return View();

            var forecastMonths = Enumerable.Range(1, horizon)
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();

            var series = new List<Object>();

            foreach(var method in raw.Select(x => x.PaymentMethod).Distinct())
            {

                var history = new List<MonthlyInput>();
                for(var m = start; m < end; m = m.AddMonths(1))
                {
                    var count = raw.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month && x.PaymentMethod == method)?.Count ?? 0;

                    history.Add(new MonthlyInput { OrderCount = count });
                }
                int windowSize = history.Count > 36 ? 12 : 3;

                var pipeline = _mLContext.Forecasting.ForecastBySsa(
                    outputColumnName: nameof(MonthlyOutput.Forecast),
                    inputColumnName: nameof(MonthlyInput.OrderCount),
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
                    method = method,
                    values = prediction.Forecast.Select(v => Math.Round(Math.Max(0f, v)))
                });
            }

            ViewBag.MonthLabels = JsonSerializer.Serialize(forecastMonths.Select(m => m.ToString("MMM yy", tr)));
            // ["Oca 26", "Şub 26", ..., "Ara 26"]

            ViewBag.PaymentSeries = JsonSerializer.Serialize(series);

            return View();

        }

        public async Task<IActionResult> GermanyCityForecast()
        {
            var horizon = 12;
            var country = "Almanya";
            var tr = new CultureInfo("tr-TR");

            var start = new DateTime(2023, 1, 1);
            var end = new DateTime(2026, 1, 1);
            var lastDataMonth = end.AddMonths(-1);

            var topCities = await _context.Orders.Where(x => x.OrderDate >= start && x.OrderDate < end  && x.Customer.CustomerCountry == country)
                .GroupBy(x => x.Customer.CustomerCity )
                .Select(g => new
                {
                    City = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .Select(x=>x.City)
                .ToListAsync();

            if (!topCities.Any())
                return View();

            var raw = await _context.Orders.Where(x => x.OrderDate >= start && x.OrderDate < end && x.Customer.CustomerCountry == country && topCities.Contains(x.Customer.CustomerCity))
                .GroupBy(x => new { x.Customer.CustomerCity, x.OrderDate.Year, x.OrderDate.Month })
                .Select(g => new
                {
                    City = g.Key.CustomerCity,
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                }).ToListAsync();

            if (!raw.Any())
                return View();

            var forecastMonths = Enumerable.Range(1, horizon)
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();


            var series = new List<object>();

            foreach(var city in topCities)
            {
                var history = new List<MonthlyInput>();

                for(var m=start; m<end; m = m.AddMonths(1))
                {
                    var count = raw.FirstOrDefault(x => x.City == city && x.Year == m.Year && x.Month == m.Month)?.Count ?? 0;

                    history.Add(new MonthlyInput{ OrderCount = count });
                }

                int windowSize = history.Count >= 36 ? 12 : 3;
                var pipeline = _mLContext.Forecasting.ForecastBySsa(
                    inputColumnName: nameof(MonthlyInput.OrderCount),
                    outputColumnName: nameof(MonthlyOutput.Forecast),
                    seriesLength:Math.Min(history.Count,windowSize*2),
                    windowSize:windowSize,
                    trainSize:history?.Count ?? 0,
                    horizon: horizon
                    );

                var dataView = _mLContext.Data.LoadFromEnumerable(history);
                var model = pipeline.Fit(dataView);
                var engine = model.CreateTimeSeriesEngine<MonthlyInput, MonthlyOutput>(_mLContext);
                var prediction = engine.Predict();


                series.Add(new
                {
                    method = city,
                    values = prediction.Forecast.Select(v => Math.Round(Math.Max(0f,v)))
                });
            }

            ViewBag.MonthLabels = JsonSerializer.Serialize(forecastMonths.Select(x => x.ToString("MMM yy", tr)));
            ViewBag.Cities = JsonSerializer.Serialize(series);
            return View();
        }

        public async Task<IActionResult> TurkeyCityForecast()
        {
            var horizon = 12;
            var tr = new CultureInfo("tr-TR");
            var country = "Türkiye";
            var start = new DateTime(2023, 1, 1);
            var end = new DateTime(2026, 1, 1);
            var lastDataMonth = end.AddMonths(-1);


            var TopCities=await _context.Orders.Where( x=> x.OrderDate>=start && x.OrderDate<end && x.Customer.CustomerCountry==country)
                .GroupBy(x=>x.Customer.CustomerCity)
                .Select(g => new
                {
                    City=g.Key,
                    Count=g.Count()
                })
                .OrderByDescending(x=>x.Count)
                .Take(6)
                .Select(x=>x.City)
                .ToListAsync();

            if (!TopCities.Any())
                return View();

            var raw = await _context.Orders.Where(x => x.OrderDate >= start && x.OrderDate < end && x.Customer.CustomerCountry == country && TopCities.Contains(x.Customer.CustomerCity))
                .GroupBy(x => new { x.Customer.CustomerCity, x.OrderDate.Year, x.OrderDate.Month })
                .Select(g => new
                {
                    City = g.Key.CustomerCity,
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                }).ToListAsync();

            if (!raw.Any())
                return View();

            var forecastMonths = Enumerable.Range(1, horizon)
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();

            var series = new List<object>();
           foreach(var city in TopCities)
            {
                var history = new List<MonthlyInput>();

                for(var m = start; m < end; m = m.AddMonths(1))
                {
                    var count = raw.FirstOrDefault(x => x.City == city && x.Year == m.Year && x.Month == m.Month)?.Count ?? 0;

                    history.Add(new MonthlyInput{ OrderCount = count });
                }

                int windowSize = history.Count >= 36 ? 12 : 3;
                var pipeline = _mLContext.Forecasting.ForecastBySsa(

                    inputColumnName:nameof(MonthlyInput.OrderCount),
                    outputColumnName:nameof(MonthlyOutput.Forecast),
                    seriesLength:Math.Min(history.Count,windowSize*2),
                    windowSize:windowSize,
                    trainSize:history?.Count ?? 0,
                    horizon:horizon
                    );

                var dataView = _mLContext.Data.LoadFromEnumerable(history);
                var model = pipeline.Fit(dataView);
                var engine = model.CreateTimeSeriesEngine<MonthlyInput, MonthlyOutput>(_mLContext);
                var prediction = engine.Predict();

                series.Add(new
                {
                    method = city,
                    values = prediction.Forecast.Select(v => Math.Round(Math.Max(0f,v)))
                });
            }

            ViewBag.MonthLabels = JsonSerializer.Serialize(forecastMonths.Select(x=>x.ToString("MMM yy",tr)));
            ViewBag.Cities = JsonSerializer.Serialize(series);


            return View();
        }

        public async Task<IActionResult> SpainCityForecast()
        {
            var horizon = 12;
            var tr = new CultureInfo("tr-TR");
            var country = "İspanya";
            var start = new DateTime(2023, 1, 1);
            var end = new DateTime(2026, 1, 1);
            var lastDataMonth = end.AddMonths(-1);

            var topCities = await _context.Orders
                .Where(x => x.OrderDate >= start && x.OrderDate < end &&  x.Customer.CustomerCountry!=null && x.Customer.CustomerCountry== country)
                .GroupBy(x => x.Customer.CustomerCity)
                .Select(g => new
                {
                    City = g.Key,
                    Count = g.Count()
                }).OrderByDescending(x => x.Count)
                .Take(6)
                .Select(x=>x.City)
                .ToListAsync();

            if (!topCities.Any())
                return View();


            var raw = await _context.Orders
                .Where(x => x.OrderDate >= start && x.OrderDate < end && x.Customer.CustomerCountry == country && topCities.Contains(x.Customer.CustomerCity))
                .GroupBy(x => new { x.Customer.CustomerCity, x.OrderDate.Year, x.OrderDate.Month })
                .Select(g => new
                {
                    City = g.Key.CustomerCity,
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                }).ToListAsync();

            if (!raw.Any())
                return View();

            var forecastMonths = Enumerable.Range(1, horizon)
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();


            var series = new List<Object>();

            foreach(var city in topCities)
            {
                var history = new List<MonthlyInput>();

                for(var m = start; m < end; m = m.AddMonths(1))
                {
                    var count = raw.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month && x.City == city)?.Count ?? 0;

                    history.Add(new MonthlyInput { OrderCount = count });
                }

                
                int windowSize = history.Count >= 36 ? 12 : 3;

                var pipeline = _mLContext.Forecasting.ForecastBySsa(
                    inputColumnName: nameof(MonthlyInput.OrderCount),
                    outputColumnName: nameof(MonthlyOutput.Forecast),
                    trainSize: history.Count,
                    windowSize:windowSize,
                    seriesLength: Math.Min(history.Count, windowSize * 2),
                    horizon: horizon
                    );

                var dataView = _mLContext.Data.LoadFromEnumerable(history);
                var model = pipeline.Fit(dataView);
                var engine = model.CreateTimeSeriesEngine<MonthlyInput, MonthlyOutput>(_mLContext);
                var prediction=engine.Predict();


                series.Add(new
                {
                    method = city,
                    values = prediction.Forecast.Select(v=>Math.Round(Math.Max(0f,v)))
                });
            }
            ViewBag.MonthLabels = JsonSerializer.Serialize(forecastMonths.Select(x => x.ToString("MMM yy", tr)));
            ViewBag.Cities = JsonSerializer.Serialize(series);
            return View();
        }
        public async Task<IActionResult> PolandCityForecast()
        {
            var horizon = 12;
            var tr = new CultureInfo("tr-TR");
            var country = "Polonya";
            var start = new DateTime(2023, 1, 1);
            var end = new DateTime(2026, 1, 1);
            var lastDataMonth = end.AddMonths(-1);

            var topCities = await _context.Orders.Where(x => x.OrderDate >= start && x.OrderDate < end && x.Customer.CustomerCountry != null && x.Customer.CustomerCountry == country)
                .GroupBy(x => x.Customer.CustomerCity)
                .Select(g => new
                {
                    City = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(6)
                .Select(x => x.City)
                .ToListAsync();

            if (!topCities.Any())
                return View();

            var raw = await _context.Orders.Where(x => x.OrderDate >= start && x.OrderDate < end && topCities.Contains(x.Customer.CustomerCity))
                .GroupBy(x => new { x.Customer.CustomerCity, x.OrderDate.Year, x.OrderDate.Month })
                .Select(g => new
                {
                    City = g.Key.CustomerCity,
                    g.Key.Year,
                    g.Key.Month,
                    Count = g.Count()
                }).ToListAsync();

            if (!raw.Any())
                return View();



            var forecastMonths = Enumerable.Range(1, horizon)
                .Select(x => lastDataMonth.AddMonths(x))
                .ToList();


            var series = new List<object>();


            foreach(var city in topCities)
            {
                var history = new List<MonthlyInput>();

                for(var m = start; m < end; m = m.AddMonths(1))
                {
                    var count = raw.FirstOrDefault(x => x.Year == m.Year && x.Month == m.Month && x.City == city)?.Count ?? 0;

                    history.Add(new MonthlyInput { OrderCount = count });
                }

                var windowSize = history.Count >= 36 ? 12 : 3;
                var pipeline = _mLContext.Forecasting.ForecastBySsa(
                    inputColumnName:nameof(MonthlyInput.OrderCount),
                    outputColumnName:nameof(MonthlyOutput.Forecast),
                    trainSize:history.Count,
                    seriesLength: Math.Min(history.Count, windowSize * 2),
                    horizon:horizon,
                    windowSize: windowSize
                    );

                var dataView = _mLContext.Data.LoadFromEnumerable(history);
                var model = pipeline.Fit(dataView);
                var engine = model.CreateTimeSeriesEngine<MonthlyInput, MonthlyOutput>(_mLContext);
                var prediction = engine.Predict(); // Horizondan Gelen Sayı Kadar Tahmin Döndürür.


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