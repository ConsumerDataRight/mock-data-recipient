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
        public SqlDataAccess _sqlDataAccess { get; }

        public SqlConsentsRepository(IConfiguration config, RecipientDatabaseContext recipientDatabaseContext)
        {
            _config = config;
            _sqlDataAccess = new SqlDataAccess(_config, recipientDatabaseContext);
        }

        public async Task<ConsentArrangement> GetConsentByArrangement(string cdrArrangementId)
        {            
            return await _sqlDataAccess.GetConsentByArrangement(cdrArrangementId);
        }

        public async Task<IEnumerable<ConsentArrangement>> GetConsents(string clientId, string dataHolderBrandId, string userId, string industry = null)
        {                        
            // filter consents by industry.
            var cdrArrangements = await _sqlDataAccess.GetConsents(clientId, dataHolderBrandId, userId);
            return cdrArrangements.OrderByDescending(x => x.CreatedOn);
        }

        public async Task PersistConsent(ConsentArrangement consentArrangement)
        {                                   
            var existingArrangement = await GetConsentByArrangement(consentArrangement.CdrArrangementId);
            if (existingArrangement == null)
            {
                await _sqlDataAccess.InsertCdrArrangement(consentArrangement);
                return;
            }
            
            await _sqlDataAccess.UpdateCdrArrangement(consentArrangement);            
        }

        public async Task UpdateTokens(string cdrArrangementId, string idToken, string accessToken, string refreshToken)
        {                        
            var consent = await GetConsentByArrangement(cdrArrangementId);
            consent.IdToken = idToken;
            consent.AccessToken = accessToken;
            consent.RefreshToken = refreshToken;
            
            await _sqlDataAccess.UpdateCdrArrangement(consent);
        }

        public async Task DeleteConsent(string cdrArrangementId)
        {                        
            var consent = await GetConsentByArrangement(cdrArrangementId);

            if (!string.IsNullOrEmpty(consent?.CdrArrangementId))
            {
                await _sqlDataAccess.DeleteConsent(cdrArrangementId);
            }            
        }

        public async Task<bool> RevokeConsent(string cdrArrangementId, string dataHolderBrandId)
        {                        
            var consent = await GetConsentByArrangement(cdrArrangementId);
            
            if (!string.IsNullOrEmpty(consent?.CdrArrangementId) && 
                string.Equals(consent?.DataHolderBrandId, dataHolderBrandId, StringComparison.OrdinalIgnoreCase))
            {
                await _sqlDataAccess.DeleteConsent(cdrArrangementId);
                return true;
            }
            return false;
        }        
    }
}
