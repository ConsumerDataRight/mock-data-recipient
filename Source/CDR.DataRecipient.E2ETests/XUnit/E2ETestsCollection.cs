using Xunit;

namespace CDR.DataRecipient.E2ETests
{
    // Test method ordering (AlphabeticalOrderer) is applied via [TestMethodOrderer] on BaseTest,
    // since ITestMethodOrderer/[TestMethodOrderer] only supports Assembly/Class targets, not
    // collection definitions.
    [CollectionDefinition("E2ETests")]
    public class E2ETestsCollection
    {
    }
}
