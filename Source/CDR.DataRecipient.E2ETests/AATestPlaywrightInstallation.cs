using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    public class AATestPlaywrightInstallation : BaseTest_v2, IClassFixture<TestFixture>
    {
        [Fact]
        public async Task ShouldDisplayGoogleHomePage()
        {
            await Task.Delay(10000); // FIXME - MJS - This test sometimes just fails in build pipeline with error about google hanging up the connection, maybe just need to give the network a little extra time to get ready?

            await TestAsync($"{nameof(AATestPlaywrightInstallation)} - {nameof(ShouldDisplayGoogleHomePage)}", async (page) =>
            {
                // Act - Goto Google.com
                var resp = await page.GotoAsync("https://www.google.com");
                await page.ClickAsync(":nth-match(:text(\"I'm Feeling Lucky\"), 2)");

                // Assert
                Assert.NotNull(resp);
                Assert.True(resp != null && resp.Status == 200);
            });
        }
    }
}