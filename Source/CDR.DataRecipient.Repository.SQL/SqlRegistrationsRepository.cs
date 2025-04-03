using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Repository.SQL
{
    public class SqlRegistrationsRepository : IRegistrationsRepository
    {
        public SqlDataAccess SqlDataAccess { get; }

        public SqlRegistrationsRepository(IConfiguration config, RecipientDatabaseContext recipientDatabaseContext)
        {
            SqlDataAccess = new SqlDataAccess(config, recipientDatabaseContext);
        }

        public async Task<Registration> GetRegistration(string clientId, string dataHolderBrandId)
        {
            return await SqlDataAccess.GetRegistration(clientId, dataHolderBrandId);
        }

        public async Task<IEnumerable<Registration>> GetRegistrations()
        {
            return await SqlDataAccess.GetRegistrations();
        }

        public async Task<IEnumerable<Registration>> GetDcrMessageRegistrations()
        {
            return await SqlDataAccess.GetDcrMessageRegistrations();
        }

        public async Task DeleteRegistration(string clientId, string dataHolderBrandId)
        {
            var registration = await GetRegistration(clientId, dataHolderBrandId);

            // Delete existing data.
            if (!string.IsNullOrEmpty(registration?.ClientId))
            {
                await SqlDataAccess.DeleteRegistration(clientId, dataHolderBrandId);
                await SqlDataAccess.DeleteCdrArrangementData(clientId);
            }
        }

        // Check is DH id is present
        public async Task PersistRegistration(Registration registration)
        {
            var existingRegistration = await GetRegistration(registration.ClientId, registration.DataHolderBrandId);

            if (string.IsNullOrEmpty(existingRegistration?.ClientId))
            {
                await SqlDataAccess.InsertRegistration(registration);
            }
        }

        public async Task UpdateRegistration(Registration registration)
        {
            var currentRegstration = await GetRegistration(registration.ClientId, registration.DataHolderBrandId);

            // Update existing data.
            if (!string.IsNullOrEmpty(currentRegstration?.ClientId))
            {
                await SqlDataAccess.UpdateRegistration(registration);
            }
        }
    }
}
