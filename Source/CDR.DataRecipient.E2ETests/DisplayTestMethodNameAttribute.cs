using System;
using System.Reflection;
using Xunit.Sdk;

#nullable enable

namespace CDR.DataRecipient.E2ETests
{
    class DisplayTestMethodNameAttribute : BeforeAfterTestAttribute
    {
        static int count = 0;

        public override void Before(MethodInfo methodUnderTest)
        {
            Console.WriteLine($"Test #{++count} - {methodUnderTest.DeclaringType?.Name}.{methodUnderTest.Name}");
        }

        public override void After(MethodInfo methodUnderTest)
        {
        }
    }
}