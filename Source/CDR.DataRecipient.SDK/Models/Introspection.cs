using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CDR.DataRecipient.SDK.Models
{
    [Serializable]
    public class Introspection : Dictionary<string, object>
    {
        public Introspection() : base() { }

        protected Introspection(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
