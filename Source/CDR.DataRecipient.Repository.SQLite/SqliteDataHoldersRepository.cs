using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;
using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Repository.SQLite
{
    public class SqliteDataHoldersRepository : IDataHoldersRepository
    {
        protected readonly IConfiguration _config;

        public SqliteDataAccess SqliteDataAccess { get; }

        public SqliteDataHoldersRepository(IConfiguration config)
        {
            _config = config;
            SqliteDataAccess = new SqliteDataAccess(_config);
        }

        public async Task<DataHolderBrand> GetDataHolderBrand(string brandId)
        {            
            var dataHolderBrand = await SqliteDataAccess.GetDataHolderBrand(brandId);            
            return dataHolderBrand;
        }

        public async Task<IEnumerable<DataHolderBrand>> GetDataHolderBrands()
        {            
            var dataHolderBrands = await SqliteDataAccess.GetDataHolderBrands();
            return dataHolderBrands;
        }

        public async Task PersistDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands)
        {            
            //Delete existing data first then add all data
            await SqliteDataAccess.PersistDataHolderBrands(dataHolderBrands);
        }
    }
}
