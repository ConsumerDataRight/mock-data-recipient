using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDR.DataRecipient.SDK.Models
{
    public class Pkce
    {
        public string CodeVerifier { get; set; }
        public string CodeChallenge { get; set; }
        public string CodeChallengeMethod { get; private set; }

        public Pkce()
        {
            this.CodeChallengeMethod = Constants.Infosec.CODE_CHALLENGE_METHOD;
        }
    }
}
