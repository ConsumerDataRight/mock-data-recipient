using System.Net;

namespace CDR.DataRecipient.Web.Models
{
    public abstract class BaseModel
    {
        public HttpStatusCode StatusCode { get; set; }
        public string Messages { get; set; }
        public SDK.Models.ErrorList ErrorList { get; set; }

        protected BaseModel()
        {
            this.ErrorList = new SDK.Models.ErrorList();
        }
    }
}
