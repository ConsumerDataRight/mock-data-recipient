using CDR.DataRecipient.SDK.Models;
using System.Collections.Generic;
using System.Linq;

namespace CDR.DataRecipient.Web.Extensions
{
    public static class LinqExtensions
    {
        public static IEnumerable<DataHolderBrand> OrderByMockDataHolders(
            this IEnumerable<DataHolderBrand> dataHolderBrands, 
            bool mockDataHoldersFirst)
        {
            if (!mockDataHoldersFirst)
            {
                return dataHolderBrands;
            }

            // Push the "Mock Data Holder" solutions to the top of the list.
            return dataHolderBrands
                .OrderBy(x => x.BrandName.Contains("Mock Data Holder") ? 1 : 2)
                .ThenBy(x => x.BrandName);
        }
    }
}
