using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Repository.SQL
{
    public class SqlDataHoldersRepository : IDataHoldersRepository
    {
        public SqlDataHoldersRepository(IConfiguration config, RecipientDatabaseContext recipientDatabaseContext)
        {
            this.SqlDataAccess = new SqlDataAccess(config, recipientDatabaseContext);
        }

        public SqlDataAccess SqlDataAccess { get; }

        public async Task<DataHolderBrand> GetDataHolderBrand(string brandId)
        {
            var dataHolderBrand = await this.SqlDataAccess.GetDataHolderBrand(brandId);
            return dataHolderBrand;
        }

        public async Task<DataHolderBrand> GetDHBrandById(string brandId)
        {
            var dataHolderBrand = await this.SqlDataAccess.GetDHBrandById(brandId);
            return dataHolderBrand;
        }

        public async Task<IEnumerable<DataHolderBrand>> GetDataHolderBrands()
        {
            return await this.SqlDataAccess.GetDataHolderBrands();
        }

        public async Task DataHolderBrandsDelete()
        {
            // Delete existing data first then add all data
            await this.SqlDataAccess.DataHolderBrandsDelete();
        }

        public async Task<(int, int)> AggregateDataHolderBrands(IList<DataHolderBrand> dataHolderBrands)
        {
            // Aggregate Old with New data
            return await this.SqlDataAccess.AggregateDataHolderBrands(dataHolderBrands);
        }

        public async Task PersistDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands)
        {
            // Delete existing data first then add all data
            await this.SqlDataAccess.PersistDataHolderBrands(dataHolderBrands);
        }
    }
}
