using Microsoft.Extensions.Configuration;

namespace CDR.DataRecipient.Web.Configuration
{
    public static class Extensions
    {
        public static Models.Register GetRegisterConfig(this IConfiguration config, string key = ConfigurationKeys.REGISTER)
        {
            var register = new Models.Register();
            config.GetSection(key).Bind(register);

            if (string.IsNullOrEmpty(register.GetDataHolderBrandsEndpoint))
            {
                register.GetDataHolderBrandsEndpoint = $"{register.MtlsBaseUri.TrimEnd('/')}/cdr-register/v1/banking/data-holders/brands";
            }

            if (string.IsNullOrEmpty(register.GetSsaEndpoint))
            {
                register.GetSsaEndpoint = register.MtlsBaseUri.TrimEnd('/') + "/cdr-register/v1/banking/data-recipients/brands/{brandId}/software-products/{softwareProductId}/ssa";
            }

            return register;
        }

        public static Models.SoftwareProduct GetSoftwareProductConfig(this IConfiguration config, string key = ConfigurationKeys.SOFTWARE_PRODUCT)
        {
            var sp = new Models.SoftwareProduct();
            config.GetSection(key).Bind(sp);
            return sp;
        }

        public static Models.DataHolder GetDefaultDataHolderConfig(this IConfiguration config, string key = ConfigurationKeys.DEFAULT_DATA_HOLDER)
        {
            var dh = new Models.DataHolder();
            config.GetSection(key).Bind(dh);
            return dh;
        }

        public static int? GetDefaultPageSize(this IConfiguration config)
        {
            var defaultPageSize = config.GetValue<int>(ConfigurationKeys.DEFAULT_PAGE_SIZE, 25);
            if (defaultPageSize != 25)
            {
                return defaultPageSize;
            }

            return null;
        }
    }
}
