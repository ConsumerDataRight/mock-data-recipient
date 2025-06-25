using System.Linq;
using CDR.DataRecipient.Web.Features;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement.Mvc;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly IConfiguration _config;

        public SettingsController(IConfiguration config)
        {
            this._config = config;
        }

        [FeatureGate(nameof(Feature.ShowSettings))]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult Index()
        {
            var model = new SettingsModel();
            this.PopulateSettings(model);
            return this.View(model);
        }

        private void PopulateSettings(SettingsModel model)
        {
            var pattern = "MockDataRecipient:";
            var configSettings = this._config.AsEnumerable().Where(c => c.Key.StartsWith(pattern) && c.Value != null).OrderBy(c => c.Key);
            foreach (var setting in configSettings)
            {
                model.ConfigurationSettings.Add(setting.Key.Replace(pattern, string.Empty), setting.Value.StartsWith("https://") ? $"<a href=\"{setting.Value}\" target=\"_blank\">{setting.Value}</a>" : setting.Value);
            }
        }
    }
}
