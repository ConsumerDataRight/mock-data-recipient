using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataRecipient.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.Web.Controllers
{
    public class SettingsController : Controller
    {

        private readonly ILogger<SettingsController> _logger;
        private readonly IConfiguration _config;

        public SettingsController(IConfiguration config, ILogger<SettingsController> logger)
        {
            _logger = logger;
            _config = config;
        }

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
