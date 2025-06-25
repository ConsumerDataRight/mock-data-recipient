using System.Collections.Generic;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.Web.Models
{
    public class RegistrationsModel : BaseModel
    {
        public RegistrationsModel()
        {
            this.Registrations = new List<Registration>();
        }

        public IList<Registration> Registrations { get; set; }

        public HttpRequestModel RegistrationRequest { get; set; }
    }
}
