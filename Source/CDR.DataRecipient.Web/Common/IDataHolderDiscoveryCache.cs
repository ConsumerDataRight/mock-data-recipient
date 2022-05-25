using CDR.DataRecipient.SDK.Models;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Common
{
    public interface IDataHolderDiscoveryCache
    {
        Task<OidcDiscovery> GetOidcDiscoveryByBrandId(string dataHolderBrandId);
        Task<OidcDiscovery> GetOidcDiscoveryByInfoSecBaseUri(string infosecBaseUri);
    }
}