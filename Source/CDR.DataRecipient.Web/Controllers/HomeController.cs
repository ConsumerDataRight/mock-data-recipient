using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private static HttpClient client = new HttpClient();
        private readonly IConfiguration _config;

        public HomeController(IConfiguration config)
        {
            this._config = config;
        }

        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public async Task<IActionResult> Index()
        {
            try
            {
                var homePageContentUrl = this._config.GetValue<string>(Constants.Content.HomepageContentUrl);
                var footerContentUrl = this._config.GetValue<string>(Constants.Content.FooterContentUrl);

                this.ViewBag.HomepageContent = string.Empty;
                this.ViewBag.FooterContent = string.Empty;

                if (!string.IsNullOrEmpty(homePageContentUrl))
                {
                    var result = await client.GetAsync(homePageContentUrl);
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        this.ViewBag.HomepageContent = result.Content.ReadAsStringAsync().Result;
                    }
                }

                if (!string.IsNullOrEmpty(footerContentUrl))
                {
                    var result = await client.GetAsync(footerContentUrl);
                    if (result.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        this.ViewBag.FooterContent = result.Content.ReadAsStringAsync().Result;
                    }
                }

                return this.View();
            }
            catch (Exception)
            {
                var msg = $"Unable to load the required content";
                return this.View("Error", new ErrorViewModel { Message = msg });
            }
        }

        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult About()
        {
            return this.View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult Error()
        {
            return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier });
        }
    }
}
