using System.Collections.Generic;
using CDR.DataRecipient.Models;

namespace CDR.DataRecipient.Web.Models
{
    public class ConsentsModel : BaseModel
    {
        public IEnumerable<ConsentArrangement> ConsentArrangements { get; set; }
    }
}
