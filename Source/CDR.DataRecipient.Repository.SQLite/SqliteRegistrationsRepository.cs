using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Repository.SQLite
{
    public class SqliteRegistrationsRepository : IRegistrationsRepository
    {
        protected readonly IConfiguration _config;

        public SqliteDataAccess SqliteDataAccess { get; }

        public SqliteRegistrationsRepository(IConfiguration config) 
        {
            _config = config;
            SqliteDataAccess = new SqliteDataAccess(_config);
        }

        public async Task<Registration> GetRegistration(string clientId)
        {                        
            var registration = await SqliteDataAccess.GetRegistration(clientId);
            return registration;            
        }

        public async Task<IEnumerable<Registration>> GetRegistrations()
        {            
            var registration = await SqliteDataAccess.GetRegistrations();
            return registration;
        }

        public async Task DeleteRegistration(string clientId)
        {                        
            var registration = await GetRegistration(clientId);

            //Delete existing data. 
            if (!string.IsNullOrEmpty(registration?.ClientId))
            {
                await SqliteDataAccess.DeleteRegistration(clientId);
            }
        }

        public async Task PersistRegistration(Registration _registration)
        {            
            var registration = await GetRegistration(_registration.ClientId);

            if (string.IsNullOrEmpty(registration?.ClientId))
            {
                await SqliteDataAccess.InsertRegistration(_registration);
            }
            return;
        }

        public async Task UpdateRegistration(Registration registration)
        {            
            var _registration = await GetRegistration(registration.ClientId);

            //Update existing data. 
            if (!string.IsNullOrEmpty(_registration?.ClientId))
            {
                await SqliteDataAccess.UpdateRegistration(registration);
            }
        }
    }
}
