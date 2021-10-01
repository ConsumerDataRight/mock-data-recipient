using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.Web.Common
{
    public interface IDataHolderDiscoveryCache
    {
        Task<OidcDiscovery> GetOidcDiscoveryByBrandId(string dataHolderBrandId);
        Task<OidcDiscovery> GetOidcDiscoveryByInfoSecBaseUri(string infosecBaseUri);
    }
}
