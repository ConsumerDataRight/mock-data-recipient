using CDR.DataRecipient.SDK.Enumerations;

namespace CDR.DataRecipient.SDK.Extensions
{
    public static class IndustryExtensions
    {
        public static string ToPath(this Industry industry)
        {
            return industry.ToString().ToLower();
        }
    }
}
