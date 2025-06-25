using CDR.DataRecipient.SDK.Models;
using Microsoft.AspNetCore.Mvc;

namespace CDR.DataRecipient.Web.Models
{
    public class ConsentCallbackGetModel
    {
        [FromQuery(Name = "response")]
        public string Response { get; set; }
    }
}
