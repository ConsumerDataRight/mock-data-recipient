using Microsoft.Data.SqlClient;
using System;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class TestFixture : IAsyncLifetime
    {
        public Task InitializeAsync()
        {
            // Ensure that Playwright has been fully installed.
            Microsoft.Playwright.Program.Main(new string[] { "install" });
            Microsoft.Playwright.Program.Main(new string[] { "install-deps" });

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}