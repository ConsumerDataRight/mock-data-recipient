using CDR.DataRecipient.SDK;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace CDR.DataRecipient.Web.Filters
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
	public class MustConsumeAttribute : ActionFilterAttribute
	{
		private readonly string _contentType;

		public MustConsumeAttribute(string contentType)
		{
			_contentType = contentType;
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			var contentType = context.HttpContext.Request.Headers["Content-Type"];
			if (contentType != _contentType)
			{
				context.Result = new BadRequestObjectResult(new ErrorListModel(Constants.ErrorCodes.InvalidHeader, Constants.ErrorTitles.InvalidHeader, string.Empty));
			}

			base.OnActionExecuting(context);
		}
	}
}
