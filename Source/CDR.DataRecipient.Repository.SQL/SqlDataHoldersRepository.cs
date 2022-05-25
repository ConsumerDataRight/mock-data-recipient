using CDR.DataRecipient.Infrastructure;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Repository.SQL
{
    public class SqlDataHoldersRepository : IDataHoldersRepository
    {
        protected readonly IConfiguration _config;
        public SqlDataAccess _sqlDataAccess { get; }

        public SqlDataHoldersRepository(IConfiguration config, RecipientDatabaseContext recipientDatabaseContext)
        {
            _config = config;
            _sqlDataAccess = new SqlDataAccess(_config, recipientDatabaseContext);
        }

        public async Task<DataHolderBrand> GetDataHolderBrand(string brandId)
        {            
            var dataHolderBrand = await _sqlDataAccess.GetDataHolderBrand(brandId);            
            return dataHolderBrand;
        }

        public async Task<IEnumerable<DataHolderBrand>> GetDataHolderBrands()
        {            
            return await _sqlDataAccess.GetDataHolderBrands();
        }

        public async Task DataHolderBrandsDelete()
        {
            // Delete existing data first then add all data
            await _sqlDataAccess.DataHolderBrandsDelete();
        }

        public async Task<(int, int)> AggregateDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands)
        {
            // Aggregate Old with New data
            return await _sqlDataAccess.AggregateDataHolderBrands(dataHolderBrands);
        }

        public async Task PersistDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands)
        {
            // Delete existing data first then add all data
            await _sqlDataAccess.PersistDataHolderBrands(dataHolderBrands);
        }
    }
}