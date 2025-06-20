using System.Collections.Generic;

namespace CDR.DataRecipient.Web.Models
{
    public class SettingsModel : BaseModel
    {
        public SettingsModel()
        {
            this.ConfigurationSettings = new Dictionary<string, string>();
        }

        public IDictionary<string, string> ConfigurationSettings { get; set; }
    }
}
