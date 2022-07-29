using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;
        private static HttpClient client = new HttpClient();

        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var homePageContentUrl = _config.GetValue<string>(Constants.Content.HomepageContentUrl);
                var footerContentUrl = _config.GetValue<string>(Constants.Content.FooterContentUrl);

                ViewBag.HomepageContent = "";
                ViewBag.FooterContent = "";

                if (!string.IsNullOrEmpty(homePageContentUrl))
                {
                    var result = await client.GetAsync(homePageContentUrl);
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        ViewBag.HomepageContent = result.Content.ReadAsStringAsync().Result;
                }

                if (!string.IsNullOrEmpty(footerContentUrl))
                {
                    var result = await client.GetAsync(footerContentUrl);
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                        ViewBag.FooterContent = result.Content.ReadAsStringAsync().Result;
                }

                return View();
            }
            catch (Exception)
            {
                var msg = $"Unable to load the required content";
                return View("Error", new ErrorViewModel { Message = msg });
            }
        }

        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}