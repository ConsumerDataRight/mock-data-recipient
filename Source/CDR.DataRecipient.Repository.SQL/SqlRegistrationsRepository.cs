using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Repository.SQL
{
    public class SqlRegistrationsRepository : IRegistrationsRepository
    {
        public SqlRegistrationsRepository(IConfiguration config, RecipientDatabaseContext recipientDatabaseContext)
        {
            this.SqlDataAccess = new SqlDataAccess(config, recipientDatabaseContext);
        }

        public SqlDataAccess SqlDataAccess { get; }

        public async Task<Registration> GetRegistration(string clientId, string dataHolderBrandId)
        {
            return await this.SqlDataAccess.GetRegistration(clientId, dataHolderBrandId);
        }

        public async Task<IEnumerable<Registration>> GetRegistrations()
        {
            return await this.SqlDataAccess.GetRegistrations();
        }

        public async Task<IEnumerable<Registration>> GetDcrMessageRegistrations()
        {
            return await this.SqlDataAccess.GetDcrMessageRegistrations();
        }

        public async Task DeleteRegistration(string clientId, string dataHolderBrandId)
        {
            var registration = await this.GetRegistration(clientId, dataHolderBrandId);

            // Delete existing data.
            if (!string.IsNullOrEmpty(registration?.ClientId))
            {
                await this.SqlDataAccess.DeleteRegistration(clientId, dataHolderBrandId);
                await this.SqlDataAccess.DeleteCdrArrangementData(clientId);
            }
        }

        // Check is DH id is present
        public async Task PersistRegistration(Registration registration)
        {
            var existingRegistration = await this.GetRegistration(registration.ClientId, registration.DataHolderBrandId);

            if (string.IsNullOrEmpty(existingRegistration?.ClientId))
            {
                await this.SqlDataAccess.InsertRegistration(registration);
            }
        }

        public async Task UpdateRegistration(Registration registration)
        {
            var currentRegstration = await this.GetRegistration(registration.ClientId, registration.DataHolderBrandId);

            // Update existing data.
            if (!string.IsNullOrEmpty(currentRegstration?.ClientId))
            {
                await this.SqlDataAccess.UpdateRegistration(registration);
            }
        }
    }
}
