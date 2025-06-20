namespace CDR.DataRecipient.SDK.Models
{
    public class Pkce
    {
        public Pkce()
        {
            this.CodeChallengeMethod = Constants.Infosec.CODE_CHALLENGE_METHOD;
        }

        public string CodeVerifier { get; set; }

        public string CodeChallenge { get; set; }

        public string CodeChallengeMethod { get; private set; }
    }
}
