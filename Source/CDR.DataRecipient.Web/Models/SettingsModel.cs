using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Models
{
    public class SettingsModel : BaseModel
    {
        public IDictionary<string, string> ConfigurationSettings { get; set; }

        public SettingsModel()
        {
            this.ConfigurationSettings = new Dictionary<string, string>();
        }
    }
}
