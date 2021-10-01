using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
#if DISABLED    
    public class TestPlaywrightInstallation : BaseTest
    {
        [Fact]
        public static async Task ShouldDisplayGoogleHomePage()
        {
            await TestAsync($"{nameof(TestPlaywrightInstallation)} - {nameof(ShouldDisplayGoogleHomePage)}", async (page) =>
            {
                // Act - Goto Google.com
                await page.GotoAsync("https://www.google.com");
            });
        }
    }
#endif
}