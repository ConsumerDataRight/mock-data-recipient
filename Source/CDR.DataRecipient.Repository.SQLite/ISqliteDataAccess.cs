using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.Repository.SQLite
{
    public interface ISqliteDataAccess
    {
        bool SqliteCreateDatabase();        
        bool RecreateDatabaseWithForTests();
        Task DeleteCdrArrangementData();
        Task DeleteRegistrationData();        
        Task<DataHolderBrand> GetDataHolderBrand(string brandId);
        Task<IEnumerable<DataHolderBrand>> GetDataHolderBrands();
        Task PersistDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands);
    }
}