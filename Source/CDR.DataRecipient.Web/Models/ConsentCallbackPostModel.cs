using Microsoft.AspNetCore.Mvc;

namespace CDR.DataRecipient.Web.Models
{
    public class ConsentCallbackPostModel
    {
        [FromForm(Name = "code")]
        public string Code { get; set; }

        [FromForm(Name = "state")]
        public string State { get; set; }

        [FromForm(Name = "response")]
        public string Response { get; set; }

        [FromForm(Name = "error")]
        public string Error { get; set; }

        [FromForm(Name = "error_description")]
        public string ErrorDescription { get; set; }

        [FromForm(Name = "error_code")]
        public string ErrorCode { get; set; }
    }
}
