using System;

namespace CDR.DataRecipient.Exceptions
{
    public class MissingClaimException : Exception
    {
        public MissingClaimException()
        {
        }

        public MissingClaimException(string claimName)
            : base($"{claimName} not found for user")
        {
        }

        public MissingClaimException(string claimName, Exception innerException)
            : base($"{claimName} not found for user", innerException)
        {
        }
    }
}
