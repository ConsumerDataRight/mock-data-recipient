using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.Web.Common;
using Microsoft.Extensions.Configuration;
using System;

namespace CDR.DataRecipient.Web.Extensions
{
    public static class Extensions
    {
        public static Register GetRegisterConfig(
            this IConfiguration config,
            string key = Constants.ConfigurationKeys.Register.Root,
            string industry = null)
        {
            var register = new Register();
            config.GetSection(key).Bind(register);

            if (string.IsNullOrEmpty(register.GetDataHolderBrandsEndpoint))
            {
                register.GetDataHolderBrandsEndpoint = register.MtlsBaseUri.TrimEnd('/') + "/cdr-register/v1/{industry}/data-holders/brands";
            }

            if (string.IsNullOrEmpty(register.GetSsaEndpoint))
            {
                register.GetSsaEndpoint = register.MtlsBaseUri.TrimEnd('/') + "/cdr-register/v1/{industry}/data-recipients/brands/{brandId}/software-products/{softwareProductId}/ssa";
            }

            return register;
        }

        public static SoftwareProduct GetSoftwareProductConfig(this IConfiguration config, string key = Common.Constants.ConfigurationKeys.MockDataRecipient.SoftwareProduct.Root)
        {
            var sp = new SoftwareProduct();
            config.GetSection(key).Bind(sp);
            return sp;
        }
        public static DataHolderEndpoints GetDefaultDataHolderConfig(this IConfiguration config, string key = Common.Constants.ConfigurationKeys.MockDataRecipient.DefaultDataHolder.Root)
        {
            var dh = new DataHolderEndpoints();
            config.GetSection(key).Bind(dh);
            return dh;
        }

        public static int? GetDefaultPageSize(this IConfiguration config)
        {
            var defaultPageSize = config.GetValue<int>(Constants.ConfigurationKeys.MockDataRecipient.DefaultPageSize, 25);
            if (defaultPageSize != 25)
            {
                return defaultPageSize;
            }

            return null;
        }

        public static DateTime AttemptValidateCdrArrangementJwtFromDate(this IConfiguration config)
        {
            var obligationDate = config.GetValue<string>(Constants.ConfigurationKeys.MockDataRecipient.AttemptValidateCdrArrangementJwtFromDate);
            if (string.IsNullOrEmpty(obligationDate))
            {
                return new DateTime(2022, 11, 15, 0, 0, 0, DateTimeKind.Utc);
            }

            return DateTime.Parse(obligationDate);
        }

    }
}