using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CDR.DataRecipient.Web.Extensions
{
    public static class ValidationExtensions
    {
        public static string GetErrorMessage(this ModelStateDictionary modelState)
        {
            if (modelState.IsValid)
            {
                return null;
            }

            var errorMessages = modelState.Values.SelectMany(m => m.Errors).Select(e => e.ErrorMessage);
            return errorMessages.FirstOrDefault();
        }
    }
}
