using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataRecipient.SDK.Exceptions
{
    public class NoHttpsException : SecurityException
    {
        public NoHttpsException() : base("A non-https endpoint has been encountered and blocked") { }
    }
}
