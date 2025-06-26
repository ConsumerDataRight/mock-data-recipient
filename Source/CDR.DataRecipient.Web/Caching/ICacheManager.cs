using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Caching
{
    public interface ICacheManager
    {
        Task<string> GetRegisterTokenEndpoint(string oidcDiscoveryUri);
    }
}
