using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Repository.SQL
{
    public class SqlDataHoldersRepository : IDataHoldersRepository
    {
        public SqlDataAccess SqlDataAccess { get; }

        public SqlDataHoldersRepository(IConfiguration config, RecipientDatabaseContext recipientDatabaseContext)
        {
            SqlDataAccess = new SqlDataAccess(config, recipientDatabaseContext);
        }

        public async Task<DataHolderBrand> GetDataHolderBrand(string brandId)
        {
            var dataHolderBrand = await SqlDataAccess.GetDataHolderBrand(brandId);
            return dataHolderBrand;
        }

        public async Task<DataHolderBrand> GetDHBrandById(string brandId)
        {
            var dataHolderBrand = await SqlDataAccess.GetDHBrandById(brandId);
            return dataHolderBrand;
        }

        public async Task<IEnumerable<DataHolderBrand>> GetDataHolderBrands()
        {
            return await SqlDataAccess.GetDataHolderBrands();
        }

        public async Task DataHolderBrandsDelete()
        {
            // Delete existing data first then add all data
            await SqlDataAccess.DataHolderBrandsDelete();
        }

        public async Task<(int, int)> AggregateDataHolderBrands(IList<DataHolderBrand> dataHolderBrands)
        {
            // Aggregate Old with New data
            return await SqlDataAccess.AggregateDataHolderBrands(dataHolderBrands);
        }

        public async Task PersistDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands)
        {
            // Delete existing data first then add all data
            await SqlDataAccess.PersistDataHolderBrands(dataHolderBrands);
        }
    }
}
