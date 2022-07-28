using CDR.DataRecipient.SDK;

namespace CDR.DataRecipient.Web.Extensions
{
    public static class ScopeExtensions
    {
        public static string GetRegisterScope(string version, int versionThreshold)
        {
            if (string.IsNullOrEmpty(version))
            {
                return Constants.Scopes.CDR_REGISTER_BANKING;
            }

            if (int.TryParse(version, out int xv))
            {
                if (xv < versionThreshold)
                {
                    return Constants.Scopes.CDR_REGISTER_BANKING;
                }
            }

            return Constants.Scopes.CDR_REGISTER;
        }
    }
}
