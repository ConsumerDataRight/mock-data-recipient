using System;
using CDR.DataRecipient.SDK;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CDR.DataRecipient.Web.Filters
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class MustConsumeAttribute : ActionFilterAttribute
    {
        private readonly string _contentType;

        public MustConsumeAttribute(string contentType)
        {
            this._contentType = contentType;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Request.ContentType != this._contentType)
            {
                context.Result = new BadRequestObjectResult(new ErrorListModel(Constants.ErrorCodes.InvalidHeader, Constants.ErrorTitles.InvalidHeader, string.Empty));
            }

            base.OnActionExecuting(context);
        }
    }
}
