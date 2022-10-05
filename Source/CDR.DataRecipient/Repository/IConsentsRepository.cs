using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.Models;

namespace CDR.DataRecipient.Repository
{
	public interface IConsentsRepository
    {
        Task<IEnumerable<ConsentArrangement>> GetConsents(string clientId, string dataHolderBrandId, string userId, string industry = null);
        Task<ConsentArrangement> GetConsentByArrangement(string cdrArrangementId);
        Task PersistConsent(ConsentArrangement consentArrangement);
        Task DeleteConsent(string cdrArrangementId);
        Task UpdateTokens(string cdrArrangementId, string idToken, string accessToken, string refreshToken);
        Task<bool> RevokeConsent(string cdrArrangementId, string dataHolderBrandId);
    }
}
