using Azure;
using BigDataOrdersDashboard.Context;
using BigDataOrdersDashboard.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Text.Json;

namespace BigDataOrdersDashboard.Controllers
{
    public class AdminMessageController : Controller
    {
        private readonly BigDataDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;

        public AdminMessageController(BigDataDbContext context, IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
        }
        [HttpGet]
        public async  Task<IActionResult> Index(int page=1)
        {
            if(page<1)
                page = 1;
            int pageSize = 100;
            var totalCount = await _context.Messages.CountAsync();

            var messages = await _context.Messages.Include(x=>x.Customer).OrderByDescending(x=>x.CreatedDate).Skip((page-1)*pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage= page;
            ViewBag.TotalPages = (int)(Math.Ceiling(totalCount / (double)pageSize));
            ViewBag.TotalCount=totalCount;
            return View(messages);
        }

        [HttpGet]
        public async Task<IActionResult> CreateMessage()
        {
            ViewBag.Customers = await GetCustomerList();
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateMessage(Message message)
        {
            var (isToxic, reason) = await CheckToxicity(message.MessageText);

            if (isToxic)
            {
                ViewBag.ToxicError = $"Bu mesaj {reason} içerdiği için gönderilemedi. Lütfen mesajı düzenleyip tekrar deneyin.";
                ViewBag.Customers = await GetCustomerList();
                return View(message);
            }



            message.MessageText = message.MessageText;
            message.SentimentLabel = await DetectSentiment(message.MessageText);
            message.CreatedDate = DateTime.Now;
            await _context.Messages.AddAsync(message);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await _context.Messages.FindAsync(id);
            if (message == null)
                return NotFound("Mesaj Bulunamadı");

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> UpdateMessage(int id)
        {
            var messsage = await _context.Messages.FindAsync(id);
            if(messsage==null)
                return NotFound("Mesaj Bulunamadı");

            ViewBag.Customers = await GetCustomerList();
            return View(messsage);
        }
        [HttpPost]
        public async Task<IActionResult> UpdateMessage(Message message)
        {
            var (istoxic, reason) = await CheckToxicity(message.MessageText);

            if (istoxic)
            {
                ViewBag.ToxicError = $"Bu mesaj {reason} içerdiği için gönderilemedi. Lütfen mesajı düzenleyip tekrar deneyin.";
                ViewBag.Customers = await GetCustomerList();
                return View(message);
            }

            message.SentimentLabel = await DetectSentiment(message.MessageText);
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetCustomerList()
        {
            return await _context.Customers
                .OrderBy(x => x.CustomerName)
                .Select(x => new SelectListItem
                {
                    Value = x.CustomerId.ToString(),
                    Text = x.CustomerName + " " + x.CustomerSurname + " (#" + x.CustomerId + ")"
                }).ToListAsync();
        }

        private async Task<(bool isToxic, string reason)> CheckToxicity(string text)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("OpenAI");

                var messageBody = new
                {
                    model = "omni-moderation-latest",
                    input = text
                };

                var response = await client.PostAsJsonAsync("moderations", messageBody);

                if (!response.IsSuccessStatusCode)
                    return (false, "");

                var responseString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseString);

                var result = doc.RootElement.GetProperty("results")[0];


                if (!result.GetProperty("flagged").GetBoolean())
                    return (false, "");

                var categoryNames = new Dictionary<string, string>
        {
            { "harassment", "hakaret" },
            { "harassment/threatening", "tehdit" },
            { "hate", "nefret söylemi" },
            { "hate/threatening", "tehdit içeren nefret söylemi" },
            { "violence", "şiddet" },
            { "sexual", "müstehcen içerik" }
        };


                foreach (var category in result.GetProperty("categories").EnumerateObject())
                {
                    if (category.Value.GetBoolean())
                    {
                        string reason = categoryNames.ContainsKey(category.Name) ? categoryNames[category.Name] : "uygunsuz içerik";
                        return (true, reason);
                    }
                }

                return (true, "uygunsuz içerik");
            }
            catch (Exception)
            {
                return (false, "");
            }
        }

        private async Task<string> DetectSentiment(string text)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("OpenAI");
                var messageBody = new
                {
                    model = "gpt-4o-mini",
                    messages = new[]
                    {
                new { role = "system", content = "Verilen müşteri mesajının duygusunu belirle. Sadece şu üç kelimeden birini yaz: Positive, Neutral, Negative." },
                new { role = "user", content = text }
                }
                };

                var responseMessage = await client.PostAsJsonAsync("chat/completions", messageBody);
                var responseString = await responseMessage.Content.ReadAsStringAsync();

                if (!responseMessage.IsSuccessStatusCode)
                    return "Neutral";

                using var doc = JsonDocument.Parse(responseString);

                string label = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (label == "Positive" || label == "Negative" || label == "Neutral")
                    return label;

                return "Neutral";
            }
            catch (Exception)
            {
                return "Neutral";
            }

          
        }

    }
}