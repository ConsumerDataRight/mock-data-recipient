﻿using System.Collections.Generic;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.Repository
{
    public interface IDataHoldersRepository
    {
        Task<IEnumerable<DataHolderBrand>> GetDataHolderBrands();
        Task<DataHolderBrand> GetDataHolderBrand(string brandId);
        Task DataHolderBrandsDelete();
        Task<(int, int)> AggregateDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands);
        Task PersistDataHolderBrands(IEnumerable<DataHolderBrand> dataHolderBrands);
    }
}
