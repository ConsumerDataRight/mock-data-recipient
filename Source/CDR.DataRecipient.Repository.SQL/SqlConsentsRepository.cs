using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Repository.SQL
{
    public class SqlConsentsRepository : IConsentsRepository
    {
        protected readonly IConfiguration _config;

        public SqlDataAccess SqlDataAccess { get; }

        public SqlConsentsRepository(IConfiguration config, RecipientDatabaseContext recipientDatabaseContext)
        {
            _config = config;
            SqlDataAccess = new SqlDataAccess(_config, recipientDatabaseContext);
        }

        public async Task<ConsentArrangement> GetConsentByArrangement(string cdrArrangementId)
        {
            return await SqlDataAccess.GetConsentByArrangement(cdrArrangementId);
        }

        public async Task<IEnumerable<ConsentArrangement>> GetConsents(string clientId, string dataHolderBrandId, string userId, string industry = null)
        {
            // filter consents by industry.
            var cdrArrangements = await SqlDataAccess.GetConsents(clientId, dataHolderBrandId, userId);
            return cdrArrangements.OrderByDescending(x => x.CreatedOn);
        }

        public async Task PersistConsent(ConsentArrangement consentArrangement)
        {
            var existingArrangement = await GetConsentByArrangement(consentArrangement.CdrArrangementId);
            if (existingArrangement == null)
            {
                await SqlDataAccess.InsertCdrArrangement(consentArrangement);
                return;
            }

            await SqlDataAccess.UpdateCdrArrangement(consentArrangement);
        }

        public async Task UpdateTokens(string cdrArrangementId, string idToken, string accessToken, string refreshToken)
        {
            var consent = await GetConsentByArrangement(cdrArrangementId);
            consent.IdToken = idToken;
            consent.AccessToken = accessToken;
            consent.RefreshToken = refreshToken;

            await SqlDataAccess.UpdateCdrArrangement(consent);
        }

        public async Task DeleteConsent(string cdrArrangementId)
        {
            var consent = await GetConsentByArrangement(cdrArrangementId);

            if (!string.IsNullOrEmpty(consent?.CdrArrangementId))
            {
                await SqlDataAccess.DeleteConsent(cdrArrangementId);
            }
        }

        public async Task<bool> RevokeConsent(string cdrArrangementId, string dataHolderBrandId)
        {
            var consent = await GetConsentByArrangement(cdrArrangementId);

            if (!string.IsNullOrEmpty(consent?.CdrArrangementId) &&
                string.Equals(consent.DataHolderBrandId, dataHolderBrandId, StringComparison.OrdinalIgnoreCase))
            {
                await SqlDataAccess.DeleteConsent(cdrArrangementId);
                return true;
            }

            return false;
        }
    }
}
