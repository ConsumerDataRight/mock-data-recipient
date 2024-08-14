using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class TestFixture : IAsyncLifetime
    {
        private static readonly string[] installArguments = ["install"];
        private static readonly string[] installDepsArguments = ["install-deeps"];

        public Task InitializeAsync()
        {
            // Only install Playwright if not running in container, since Dockerfile.e2e-tests already installed Playwright
            if (!BaseTest.RUNNING_IN_CONTAINER)
            {
                // Ensure that Playwright has been fully installed.
                Microsoft.Playwright.Program.Main(installArguments);
                Microsoft.Playwright.Program.Main(installDepsArguments);
            }

            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}