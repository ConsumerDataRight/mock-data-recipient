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
            // Only install Playwright if not running in container, since Dockerfile.e2e-tests already installed Playwright
            if (!BaseTest_v2.RUNNING_IN_CONTAINER)
            {
                // Ensure that Playwright has been fully installed.
                Microsoft.Playwright.Program.Main(new string[] { "install" });
                Microsoft.Playwright.Program.Main(new string[] { "install-deps" });
            }

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}