using System;
using CDR.DataRecipient.Repository;
using CDR.DataRecipient.SDK.Services.DataHolder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace CDR.DataRecipient.Web.Controllers
{
    [Route(PATH)]
    public class DataSharingCommonController : DataSharingControllerBase
    {
        private const string PATH = "data-sharing-common";

        protected override string BasePath
        {
            get
            {
                return PATH;
            }
        }

        protected override string IndustryName
        {
            get
            {
                return "Common";
            }
        }

        protected override string CdsSwaggerLocation
        {
            get
            {
                return _config["ConsumerDataStandardsSwaggerCommon"];
            }
        }

        public DataSharingCommonController(
            IConfiguration config,
            IDistributedCache cache,
            IConsentsRepository consentsRepository,
            IDataHoldersRepository dhRepository,
            IInfosecService infosecService,
            ILogger<DataSharingControllerBase> logger) : base(config, cache, consentsRepository, dhRepository, infosecService, logger)
        {
        }

        protected override JObject PrepareSwaggerJson(JObject json, Uri uri)
        {
            json["servers"][0]["url"] = $"https://{uri.Host}:{uri.Port}/{PATH}/proxy/cds-au/v1";
            return json;
        }

        /// <summary>
        /// Determine if the target request is for a public endpoint, or a resource endpoint.
        /// </summary>
        /// <param name="requestPath">Request Path</param>
        /// <returns>True if the request is for the /discovery or /banking/products endpoints, otherwise false</returns>
        protected override bool IsPublic(string requestPath)
        {
            return requestPath.Contains("/cds-au/v1/discovery", StringComparison.OrdinalIgnoreCase);
        }

    }
}
