using Newtonsoft.Json;
using System;

namespace CDR.DataRecipient.SDK.Models
{
	public class Registration
	{
		public const string IdDelimeter = "|||";

		public string DataHolderBrandId { get; set; }

		public string BrandName { get; set; }

		public string MessageState { get; set; }

		public DateTime LastUpdated { get; set; }

		[JsonProperty("client_id")]
		public string ClientId { get; set; }

		[JsonProperty("client_id_issued_at")]
		public int ClientIdIssuedAt { get; set; }

		[JsonProperty("client_description")]
		public string ClientDescription { get; set; }

		[JsonProperty("client_uri")]
		public string ClientUri { get; set; }

		[JsonProperty("org_id")]
		public string OrgId { get; set; }

		[JsonProperty("org_name")]
		public string OrgName { get; set; }

		[JsonProperty("redirect_uris")]
		public string[] RedirectUris { get; set; }

		[JsonProperty("logo_uri")]
		public string LogoUri { get; set; }

		[JsonProperty("tos_uri")]
		public string TosUri { get; set; }

		[JsonProperty("policy_uri")]
		public string PolicyUri { get; set; }

		[JsonProperty("jwks_uri")]
		public string JwksUri { get; set; }

		[JsonProperty("revocation_uri")]
		public string RevocationUri { get; set; }

		[JsonProperty("recipient_base_uri")]
		public string RecipientBaseUri { get; set; }

		[JsonProperty("token_endpoint_auth_signing_alg")]
		public string TokenEndpointAuthSigningAlg { get; set; }

		[JsonProperty("token_endpoint_auth_method")]
		public string TokenEndpointAuthMethod { get; set; }

		[JsonProperty("grant_types")]
		public string[] GrantTypes { get; set; }

		[JsonProperty("response_types")]
		public string[] ResponseTypes { get; set; }

		[JsonProperty("application_type")]
		public string ApplicationType { get; set; }

		[JsonProperty("id_token_signed_response_alg")]
		public string IdTokenSignedResponseAlg { get; set; }

		[JsonProperty("id_token_encrypted_response_alg")]
		public string IdTokenEncryptedResponseAlg { get; set; }

		[JsonProperty("id_token_encrypted_response_enc")]
		public string IdTokenEncryptedResponseEnc { get; set; }

		[JsonProperty("request_object_signing_alg")]
		public string RequestObjectSigningAlg { get; set; }

		[JsonProperty("software_statement")]
		public string SoftwareStatement { get; set; }

		[JsonProperty("software_id")]
		public string SoftwareId { get; set; }

		[JsonProperty("scope")]
		public string Scope { get; set; }


		public string GetRegistrationId()
		{
			return $"{ClientId}{IdDelimeter}{DataHolderBrandId}";
		}
		public static (string ClientId, string DataHolderBrandId) SplitRegistrationId(string id)
		{
			var idParts = id.Split(IdDelimeter);
            if (idParts == null)
            {
                return (null, null);
            }

            if (idParts != null && idParts.Length != 2)
			{
				return (null, null);
			}

			return (idParts[0], idParts[1]);
		}
	}
}
