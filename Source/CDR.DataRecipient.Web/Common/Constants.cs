namespace CDR.DataRecipient.Web.Common
{
	public class Constants
	{
		public static class Urls
		{
			public const string ClientArrangementRevokeUrl = "arrangements/revoke";
		}

		public static class ErrorCodes
		{
			public const string MissingField = "urn:au-cds:error:cds-all:Field/Missing";
			public const string InvalidHeader = "urn:au-cds:error:cds-all:Header/Invalid";
			public const string InvalidArrangement = "urn:au-cds:error:cds-all:Authorisation/InvalidArrangement";
		}

		public static class ErrorTitles
		{
			public const string MissingField = "Missing Required Field";
			public const string InvalidArrangement = "Invalid Consent Arrangement";
			public const string InvalidHeader = "Invalid Header";
		}
	}
}
