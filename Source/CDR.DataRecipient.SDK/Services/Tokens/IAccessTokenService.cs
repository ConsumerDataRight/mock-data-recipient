using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.SDK.Services.Tokens
{
    public interface IAccessTokenService
    {
        Task<Response<Token>> GetAccessToken(AccessToken accessToken);
    }
}
