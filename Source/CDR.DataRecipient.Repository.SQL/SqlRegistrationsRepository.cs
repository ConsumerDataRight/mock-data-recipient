using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Repository.SQL
{
    public class SqlRegistrationsRepository : IRegistrationsRepository
    {
        protected readonly IConfiguration _config;
        public SqlDataAccess _sqlDataAccess { get; }

        public SqlRegistrationsRepository(IConfiguration config, RecipientDatabaseContext recipientDatabaseContext) 
        {
            _config = config;
            _sqlDataAccess = new SqlDataAccess(_config, recipientDatabaseContext);
        }

        public async Task<Registration> GetRegistration(string clientId)
        {                        
            return await _sqlDataAccess.GetRegistration(clientId);
        }

        public async Task<IEnumerable<Registration>> GetRegistrations()
        {            
            return await _sqlDataAccess.GetRegistrations();
        }

        public async Task<IEnumerable<Registration>> GetDcrMessageRegistrations()
        {
            return await _sqlDataAccess.GetDcrMessageRegistrations();
        }

        public async Task DeleteRegistration(string clientId)
        {                        
            var registration = await GetRegistration(clientId);

            //Delete existing data. 
            if (!string.IsNullOrEmpty(registration?.ClientId))
            {
                await _sqlDataAccess.DeleteRegistration(clientId);
                await _sqlDataAccess.DeleteCdrArrangementData(clientId);
            }
        }

        public async Task PersistRegistration(Registration registration)
        {            
            var existingRegistration = await GetRegistration(registration.ClientId);

            if (string.IsNullOrEmpty(existingRegistration?.ClientId))
            {
                await _sqlDataAccess.InsertRegistration(registration);
            }
        }

        public async Task UpdateRegistration(Registration registration)
        {            
            var _registration = await GetRegistration(registration.ClientId);

            //Update existing data. 
            if (!string.IsNullOrEmpty(_registration?.ClientId))
            {
                await _sqlDataAccess.UpdateRegistration(registration);
            }
        }
    }
}
