using System;
using System.Runtime.Serialization;

namespace CDR.DataRecipient.Exceptions
{
    [Serializable]
    public class MissingClaimException : Exception
    {
        public MissingClaimException()
        {
        }

        public MissingClaimException(string claimName) : base($"{claimName} not found for user")
        {
        }

        public MissingClaimException(string claimName, Exception innerException) : base($"{claimName} not found for user", innerException)
        {
        }

        protected MissingClaimException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}