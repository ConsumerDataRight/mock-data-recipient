using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class TestFixture : IAsyncLifetime
    {
        public static void PatchRegister()
        {
            using var connection = new SqliteConnection(BaseTest.REGISTER_CONNECTIONSTRING);
            connection.Open();

            using var updateCommand = new SqliteCommand(@"
                    update 
                        softwareproduct
                    set 
                        recipientbaseuri = 'https://localhost:9001',
                        revocationuri = 'https://localhost:9001/revocation',
                        redirecturis = 'https://localhost:9001/consent/callback',
                        jwksuri = 'https://localhost:9001/jwks'
                    where 
                        softwareproductid = 'C6327F87-687A-4369-99A4-EAACD3BB8210'",
                connection);
            updateCommand.ExecuteNonQuery();

            using var updateCommand2 = new SqliteCommand(@"
                    update
                        endpoint
                    set 
                        publicbaseuri = 'https://localhost:8000',
                        resourcebaseuri = 'https://localhost:8002',
                        infosecbaseuri = 'https://localhost:8001'
                    where 
                        brandid = '804FC2FB-18A7-4235-9A49-2AF393D18BC7'",
                connection);
            updateCommand2.ExecuteNonQuery();
        }

        static void Purge(SqliteConnection connection, string table)
        {
            // Delete all rows from table
            using var deleteCommand = new SqliteCommand($"delete from {table}", connection);
            deleteCommand.ExecuteNonQuery();

            // Check all rows deleted
            using var selectCommand = new SqliteCommand($"select count(*) from {table}", connection);
            var count = Convert.ToInt32(selectCommand.ExecuteScalar());
            if (count != 0)
            {
                throw new Exception($"Error purging {table}");
            }
        }

        public static void PurgeMDR()
        {
            using var mdrConnection = new SqliteConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
            mdrConnection.Open();

            Purge(mdrConnection, "CdrArrangement");
            Purge(mdrConnection, "DataHolderBrand");
            Purge(mdrConnection, "Registration");
        }

        public static void PurgeMDR_CDRArrangements()
        {
            using var mdrConnection = new SqliteConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
            mdrConnection.Open();

            Purge(mdrConnection, "CdrArrangement");
        }

        // static void PurgeMDRArrangements()
        // {
        //     using var mdrConnection = new SqliteConnection(BaseTest.DATARECIPIENT_CONNECTIONSTRING);
        //     mdrConnection.Open();

        //     Purge(mdrConnection, "CdrArrangement");
        // }

        public static void PurgeMDHIdentityServer()
        {
            using var mdhIdentityServerConnection = new SqliteConnection(BaseTest.DATAHOLDER_IDENTITYSERVER_CONNECTIONSTRING);
            mdhIdentityServerConnection.Open();

            // Purge(mdhIdentityServerConnection, "ApiResourceClaims");
            // Purge(mdhIdentityServerConnection, "ApiResourceProperties");
            // Purge(mdhIdentityServerConnection, "ApiResources");
            // Purge(mdhIdentityServerConnection, "ApiResourceScopes");
            // Purge(mdhIdentityServerConnection, "ApiResourceSecrets");
            // Purge(mdhIdentityServerConnection, "ApiScopeClaims");
            // Purge(mdhIdentityServerConnection, "ApiScopeProperties");
            // Purge(mdhIdentityServerConnection, "ApiScopes");
            Purge(mdhIdentityServerConnection, "ClientClaims");
            Purge(mdhIdentityServerConnection, "ClientCorsOrigins");
            Purge(mdhIdentityServerConnection, "ClientGrantTypes");
            Purge(mdhIdentityServerConnection, "ClientIdPRestrictions");
            Purge(mdhIdentityServerConnection, "ClientPostLogoutRedirectUris");
            Purge(mdhIdentityServerConnection, "ClientProperties");
            Purge(mdhIdentityServerConnection, "ClientRedirectUris");
            Purge(mdhIdentityServerConnection, "Clients");
            Purge(mdhIdentityServerConnection, "ClientScopes");
            Purge(mdhIdentityServerConnection, "ClientSecrets");
            // Purge(mdhIdentityServerConnection, "DeviceCodes");
            // Purge(mdhIdentityServerConnection, "IdentityResourceClaims");
            // Purge(mdhIdentityServerConnection, "IdentityResourceProperties");
            // Purge(mdhIdentityServerConnection, "IdentityResources");
            Purge(mdhIdentityServerConnection, "PersistedGrants");
        }

        public Task InitializeAsync()
        {
            // DEBUG - comment out these lines - only for debugging
            // PatchRegister();  
            // PurgeMDR();
            // PurgeMDRArrangements(); 
            // PurgeMDHIdentityServer();

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}