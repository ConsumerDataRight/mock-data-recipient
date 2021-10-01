using CDR.DataRecipient.SDK.Models;

namespace CDR.DataRecipient.Web.Models
{
    public class TokenModel : BaseModel
    {
        public Response<Token> TokenResponse { get; set; }
    }
}
