using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DataAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataController : ControllerBase
    {
        private readonly DataContext _context;

        public DataController(DataContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> FetchAndSaveData()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var xmlData = await httpClient.GetStringAsync("https://www.tcmb.gov.tr/kurlar/today.xml");

                    var xDocument = XDocument.Parse(xmlData);

                    var exchangeRates = xDocument.Descendants("Currency")
                        .Where(currency =>
                        {
                            var dateValue = currency.Element("Date")?.Value;
                            if (DateTime.TryParseExact(dateValue, "MM/dd/yyyy", null, System.Globalization.DateTimeStyles.None, out var date))
                            {
                                // Sadece son 2 ay içindeki verileri al
                                return date >= DateTime.Now.AddMonths(-2);
                            }
                            return false;
                        })
                        .Select(currency => new ExchangeRate
                        {
                            CurrencyCode = currency.Element("CurrencyCode")?.Value,
                            CurrencyName = currency.Element("CurrencyName")?.Value,
                            ForexBuying = Convert.ToDecimal(currency.Element("ForexBuying")?.Value),
                            ForexSelling = Convert.ToDecimal(currency.Element("ForexSelling")?.Value),
                            BanknoteBuying = Convert.ToDecimal(currency.Element("BanknoteBuying")?.Value),
                            BanknoteSelling = Convert.ToDecimal(currency.Element("BanknoteSelling")?.Value),
                            Date = DateTime.ParseExact(currency.Element("Date")?.Value, "MM/dd/yyyy", null)
                        });

                    _context.ExchangeRates.AddRange(exchangeRates);
                    await _context.SaveChangesAsync();

                    return Ok("Data fetched and saved successfully.");
                }
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, $"Error fetching data from the source: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
    }
}