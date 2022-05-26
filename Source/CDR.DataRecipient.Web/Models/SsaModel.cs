using CDR.DataRecipient.SDK.Enumerations;
using System.ComponentModel.DataAnnotations;

namespace CDR.DataRecipient.Web.Models
{
    public class SsaModel : BaseModel
    {
        public HttpRequestModel SSARequest { get; set; }

        public string SSA { get; set; }

        [Display(Name = "Industry")]
        public Industry Industry { get; set; }

        [Display(Name = "Version")]
        public string Version { get; set; }

        [Display(Name = "Brand ID")]
        public string BrandId { get; set; }

        [Display(Name = "Software Product ID")]
        public string SoftwareProductId { get; set; }
    }
}
