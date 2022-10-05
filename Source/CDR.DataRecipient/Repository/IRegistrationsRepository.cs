using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.Repository
{
    public interface IRegistrationsRepository
    {
        Task<IEnumerable<Registration>> GetRegistrations();
        Task<IEnumerable<Registration>> GetDcrMessageRegistrations();
        Task<Registration> GetRegistration(string clientId, string dataHolderBrandId);
        Task PersistRegistration(Registration registration);
        Task DeleteRegistration(string clientId, string dataHolderBrandId);
        Task UpdateRegistration(Registration registration);
    }
}
