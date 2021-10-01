using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDR.DataRecipient.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Repository.SQLite
{
    public class SqliteConsentsRepository : IConsentsRepository
    {
        protected readonly IConfiguration _config;

        public SqliteDataAccess SqliteDataAccess { get; }

        public SqliteConsentsRepository(IConfiguration config)
        {
            _config = config;
            SqliteDataAccess = new SqliteDataAccess(_config);
        }
        public async Task<ConsentArrangement> GetConsent(string cdrArrangementId)
        {            
            var dataHolderBrand = await SqliteDataAccess.GetConsent(cdrArrangementId);
            return dataHolderBrand;
        }

        public async Task<IEnumerable<ConsentArrangement>> GetConsents()
        {                        
            var cdrArrangements = await SqliteDataAccess.GetConsents();
            return cdrArrangements.OrderByDescending(x => x.CreatedOn);
        }

        public async Task PersistConsent(ConsentArrangement consentArrangement)
        {                                   
            var existingArrangement = await GetConsent(consentArrangement.CdrArrangementId);
            if (existingArrangement == null)
            {
                
                await SqliteDataAccess.InsertCdrArrangement(consentArrangement);
                return;
            }
            
            await SqliteDataAccess.UpdateCdrArrangement(consentArrangement);            
        }

        public async Task UpdateTokens(string cdrArrangementId, string idToken, string accessToken, string refreshToken)
        {                        
            var consent = await GetConsent(cdrArrangementId);
            consent.IdToken = idToken;
            consent.AccessToken = accessToken;
            consent.RefreshToken = refreshToken;
            
            await SqliteDataAccess.UpdateCdrArrangement(consent);
        }

        public async Task DeleteConsent(string cdrArrangementId)
        {                        
            var consent = await GetConsent(cdrArrangementId);

            if (!string.IsNullOrEmpty(consent?.CdrArrangementId))
            {
                await SqliteDataAccess.DeleteConsent(cdrArrangementId);
            }            
        }

        public async Task<bool> RevokeConsent(string cdrArrangementId, string dataHolderBrandId)
        {                        
            var consent = await GetConsent(cdrArrangementId);
            
            if (!string.IsNullOrEmpty(consent?.CdrArrangementId) && 
                string.Equals(consent?.DataHolderBrandId, dataHolderBrandId, StringComparison.OrdinalIgnoreCase))
            {
                await SqliteDataAccess.DeleteConsent(cdrArrangementId);
                return true;
            }
            return false;
        }        
    }
}
