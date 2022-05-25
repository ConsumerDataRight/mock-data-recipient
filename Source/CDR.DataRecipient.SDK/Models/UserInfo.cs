using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CDR.DataRecipient.SDK.Models
{
    [Serializable]
    public class UserInfo : Dictionary<string, string>
    {
        public UserInfo() : base() { }

        protected UserInfo(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
