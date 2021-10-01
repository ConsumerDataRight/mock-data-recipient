using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.Repository
{
    public interface IRegistrationsRepository
    {
        Task<IEnumerable<Registration>> GetRegistrations();
        Task<Registration> GetRegistration(string clientId);
        Task PersistRegistration(Registration registration);
        Task DeleteRegistration(string clientId);
        Task UpdateRegistration(Registration registration);
    }
}
