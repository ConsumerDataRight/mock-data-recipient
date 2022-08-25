using CDR.DataRecipient.Exceptions;
using CDR.DataRecipient.Web.Common;
using System.Linq;
using System.Security.Claims;
using static CDR.DataRecipient.Web.Common.Constants;

namespace CDR.DataRecipient.Web.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.Claims.FirstOrDefault(c => c.Type.Equals(Constants.Claims.UserId, System.StringComparison.OrdinalIgnoreCase));
            if (userIdClaim == null)
            {
                throw new MissingClaimException(Constants.Claims.UserId);
            }
            return userIdClaim.Value;
        }

        public static string GetUserName(this ClaimsPrincipal user)
        {
            var userNameClaim = user.Claims.FirstOrDefault(c => c.Type.Equals(Constants.Claims.Name, System.StringComparison.OrdinalIgnoreCase));
            if (userNameClaim == null)
            {
                throw new MissingClaimException(Constants.Claims.Name);
            }
            return userNameClaim.Value;
        }

        public static bool IsLocal(this ClaimsPrincipal user)
        {
            return user.Identity.IsAuthenticated && user.Identity.AuthenticationType != null && user.Identity.AuthenticationType.Equals(Constants.LocalAuthentication.AuthenticationType);
        }

        public static bool IsUserNameUnknown(this ClaimsPrincipal user)
        {
            return user.GetUserName().ToLower().CompareTo(Defaults.DefaultUserName) == 0;
        }
    }
}