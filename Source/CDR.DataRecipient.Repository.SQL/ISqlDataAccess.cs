using CDR.DataRecipient.SDK.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Repository.SQL
{
    public interface ISqlDataAccess
    {
        Task DeleteCdrArrangementData();
        Task DeleteCdrArrangementData(string clientId);
        Task DeleteRegistrationData();        
        Task<DataHolderBrand> GetDataHolderBrand(string brandId);
        Task<IEnumerable<DataHolderBrand>> GetDataHolderBrands();
    }
}