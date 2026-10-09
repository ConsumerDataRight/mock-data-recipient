using System.Collections.Generic;
using System.Linq;
using Xunit.Sdk;
using Xunit.v3;

namespace CDR.DataRecipient.E2ETests.XUnit.Orderers
{
 
    public class AlphabeticalOrderer : ITestMethodOrderer
    {
        public IReadOnlyCollection<TTestMethod> OrderTestMethods<TTestMethod>(
            IReadOnlyCollection<TTestMethod> testMethods)
            where TTestMethod : notnull, ITestMethod
        {
            return testMethods
                .OrderBy(tm => tm?.MethodName ?? string.Empty)
                .ToList()
                .AsReadOnly();
        }
    }
}
