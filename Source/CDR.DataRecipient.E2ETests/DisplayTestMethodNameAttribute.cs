using System;
using System.Reflection;
using Xunit.Sdk;
using Xunit.v3;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    class DisplayTestMethodNameAttribute : BeforeAfterTestAttribute
    {
        static int count = 0;

        public override void Before(MethodInfo methodUnderTest, IXunitTest test)
        {
            Console.WriteLine($"Test #{++count} - {methodUnderTest.DeclaringType?.Name}.{methodUnderTest.Name}");
        }

        public override void After(MethodInfo methodUnderTest, IXunitTest test)
        {
        }
    }
}
