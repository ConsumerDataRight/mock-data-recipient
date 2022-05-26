using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("health")]
    public class HealthController : Controller
    {
        [HttpGet("status")]
        public IActionResult Index()
        {
            return Json(new Health() { Status = "OK" });
        }
    }
}