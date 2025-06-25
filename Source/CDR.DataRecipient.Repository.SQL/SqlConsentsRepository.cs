using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.Models;
using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Repository.SQL
{
    public class SqlConsentsRepository : IConsentsRepository
    {
        private readonly IConfiguration _config;

        public SqlConsentsRepository(IConfiguration config, RecipientDatabaseContext recipientDatabaseContext)
        {
            this._config = config;
            this.SqlDataAccess = new SqlDataAccess(this._config, recipientDatabaseContext);
        }

        public SqlDataAccess SqlDataAccess { get; }

        protected IConfiguration Config => this._config;

        public async Task<ConsentArrangement> GetConsentByArrangement(string cdrArrangementId)
        {
            return await this.SqlDataAccess.GetConsentByArrangement(cdrArrangementId);
        }

        public async Task<IEnumerable<ConsentArrangement>> GetConsents(string clientId, string dataHolderBrandId, string userId, string industry = null)
        {
            // filter consents by industry.
            var cdrArrangements = await this.SqlDataAccess.GetConsents(clientId, dataHolderBrandId, userId);
            return cdrArrangements.OrderByDescending(x => x.CreatedOn);
        }

        public async Task PersistConsent(ConsentArrangement consentArrangement)
        {
            var existingArrangement = await this.GetConsentByArrangement(consentArrangement.CdrArrangementId);
            if (existingArrangement == null)
            {
                await this.SqlDataAccess.InsertCdrArrangement(consentArrangement);
                return;
            }

            await this.SqlDataAccess.UpdateCdrArrangement(consentArrangement);
        }

        public async Task UpdateTokens(string cdrArrangementId, string idToken, string accessToken, string refreshToken)
        {
            var consent = await this.GetConsentByArrangement(cdrArrangementId);
            consent.IdToken = idToken;
            consent.AccessToken = accessToken;
            consent.RefreshToken = refreshToken;

            await this.SqlDataAccess.UpdateCdrArrangement(consent);
        }

        public async Task DeleteConsent(string cdrArrangementId)
        {
            var consent = await this.GetConsentByArrangement(cdrArrangementId);

            if (!string.IsNullOrEmpty(consent?.CdrArrangementId))
            {
                await this.SqlDataAccess.DeleteConsent(cdrArrangementId);
            }
        }

        public async Task<bool> RevokeConsent(string cdrArrangementId, string dataHolderBrandId)
        {
            var consent = await this.GetConsentByArrangement(cdrArrangementId);

            if (!string.IsNullOrEmpty(consent?.CdrArrangementId) &&
                string.Equals(consent.DataHolderBrandId, dataHolderBrandId, StringComparison.OrdinalIgnoreCase))
            {
                await this.SqlDataAccess.DeleteConsent(cdrArrangementId);
                return true;
            }

            return false;
        }
    }
}
