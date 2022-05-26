using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("oidc")]
    public class OidcController : Controller
    {
        [Route("remoteerror")]
        [HttpGet]
        public IActionResult RemoteError([FromQuery(Name = "error_message")] string errMsg)
        {
            return View("Error", new ErrorViewModel {  ErrorTitle = "Remote Error", Message = ProcessMessage(errMsg) });
        }

        [Route("autherror")]
        [HttpGet]
        public IActionResult AuthError([FromQuery(Name = "error_message")] string errMsg)
        {
            return View("Error", new ErrorViewModel { ErrorTitle = "Authentication Error", Message = ProcessMessage(errMsg) });
        }

        [Route("accesserror")]
        [HttpGet]
        public IActionResult AccessError([FromQuery(Name = "error_message")] string errMsg)
        {
            return View("Error", new ErrorViewModel { ErrorTitle = "Access Error", Message = ProcessMessage(errMsg) });
        }

        private string ProcessMessage(string errMsg)
        {
            var msg = "";
            if (!string.IsNullOrEmpty(errMsg))
            {
                var msgParts = errMsg.Split("|");
                if (msgParts.Length > 0)
                {
                    foreach (var item in msgParts)
                    {
                        if (item.Contains("error"))
                        {
                            msg = item;
                            break;
                        }
                    }
                }
            }
            return msg;
        }
    }
}