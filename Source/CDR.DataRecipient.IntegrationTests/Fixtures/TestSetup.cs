using Microsoft.Data.SqlClient;

namespace CDR.DataRecipient.IntegrationTests.Fixtures
{
    internal static class TestSetup
    {
        public static void PatchRegister()
        {
            using var connection = new SqlConnection(BaseTest.REGISTER_CONNECTIONSTRING);
            connection.Open();

            using var updateCommand = new SqlCommand($@"
                    update 
                        softwareproduct
                    set 
                        recipientbaseuri = 'https://{BaseTest.HOSTNAME_DATARECIPIENT}:9001',
                        revocationuri = 'https://{BaseTest.HOSTNAME_DATARECIPIENT}:9001/revocation',
                        redirecturis = 'https://{BaseTest.HOSTNAME_DATARECIPIENT}:9001/consent/callback',
                        jwksuri = 'https://{BaseTest.HOSTNAME_DATARECIPIENT}:9001/jwks'
                    where 
                        softwareproductid = 'C6327F87-687A-4369-99A4-EAACD3BB8210'",
                connection);
            updateCommand.ExecuteNonQuery();

            using var updateCommand2 = new SqlCommand($@"
                    update
                        endpoint
                    set 
                        publicbaseuri = 'https://{BaseTest.HOSTNAME_DATAHOLDER}:8000',
                        resourcebaseuri = 'https://{BaseTest.HOSTNAME_DATAHOLDER}:8002',
                        infosecbaseuri = 'https://{BaseTest.HOSTNAME_DATAHOLDER}:8001'
                    where 
                        brandid = '804FC2FB-18A7-4235-9A49-2AF393D18BC7'",
                connection);
            updateCommand2.ExecuteNonQuery();
        }
    }
}
