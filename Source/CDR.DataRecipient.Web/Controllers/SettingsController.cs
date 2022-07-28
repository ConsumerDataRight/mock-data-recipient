using CDR.DataRecipient.Web.Features;
using CDR.DataRecipient.Web.Filters;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement.Mvc;
using System.Linq;

namespace CDR.DataRecipient.Web.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {

        private readonly IConfiguration _config;

        public SettingsController(IConfiguration config)
        {
            _config = config;
        }

        [FeatureGate(nameof(FeatureFlags.ShowSettings))]
        [ServiceFilter(typeof(LogActionEntryAttribute))]
        public IActionResult Index()
        {
            var model = new SettingsModel();
            PopulateSettings(model);
            return View(model);
        }

        private void PopulateSettings(SettingsModel model)
        {
            var pattern = "MockDataRecipient:";
            var configSettings = _config.AsEnumerable().Where(c => c.Key.StartsWith(pattern) && c.Value != null).OrderBy(c => c.Key);
            foreach (var setting in configSettings)
            {
                model.ConfigurationSettings.Add(setting.Key.Replace(pattern, ""), setting.Value.StartsWith("https://") ? $"<a href=\"{setting.Value}\" target=\"_blank\">{setting.Value}</a>" :  setting.Value);
            }
        }
    }
}
