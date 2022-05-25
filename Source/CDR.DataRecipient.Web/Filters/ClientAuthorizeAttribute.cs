using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Net.Http.Headers;

namespace CDR.DataRecipient.Web.Filters
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
	public class ClientAuthorizeAttribute : Attribute, IAuthorizationFilter
	{
		public const string ClaimsPrincipalKey = "Client";
		public void OnAuthorization(AuthorizationFilterContext context)
		{
			var client = context.HttpContext.Items[ClaimsPrincipalKey] as ClaimsPrincipal;
			if (client == null)
			{
				// Invalid JWT
				context.Result = new JsonResult(new { error = "invalid_token" }) { StatusCode = StatusCodes.Status401Unauthorized };
				context.HttpContext.Response.Headers.Append(HeaderNames.WWWAuthenticate, "Bearer error=\"invalid_token\"");
			}
		}
	}
}
