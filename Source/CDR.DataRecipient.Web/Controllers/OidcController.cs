using System;
using System.Threading.Tasks;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route("oidc")]
    public class OidcController : Controller
    {
        public OidcController()
        {
        }

        [Route("remoteerror")]
        [HttpGet]
        public IActionResult RemoteError([FromQuery(Name = "error_message")] string errMsg)
        {
            return this.View("Error", new ErrorViewModel { ErrorTitle = "Remote Error", Message = ProcessMessage(errMsg) });
        }

        [Route("autherror")]
        [HttpGet]
        public IActionResult AuthError([FromQuery(Name = "error_message")] string errMsg)
        {
            return this.View("Error", new ErrorViewModel { ErrorTitle = "Authentication Error", Message = ProcessMessage(errMsg) });
        }

        [Route("accesserror")]
        [HttpGet]
        public IActionResult AccessError([FromQuery(Name = "error_message")] string errMsg)
        {
            return this.View("Error", new ErrorViewModel { ErrorTitle = "Access Error", Message = ProcessMessage(errMsg) });
        }

        [Route("logout")]
        [HttpGet]
        public async Task Logout()
        {
            await this.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var prop = new AuthenticationProperties()
            {
                RedirectUri = "/",
            };
            await this.HttpContext.SignOutAsync("OpenIdConnect", prop);
        }

        private static string ProcessMessage(string errMsg)
        {
            var msg = string.Empty;
            if (!string.IsNullOrEmpty(errMsg))
            {
                var msgParts = errMsg.Split("|");
                var errorMessage = Array.Find(msgParts, item => item.Contains("error"));
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    msg = errorMessage;
                }
            }

            return msg;
        }
    }
}
