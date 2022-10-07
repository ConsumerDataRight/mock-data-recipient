using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models
{
    public class OidcDiscovery
    {
        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }

        [JsonProperty("token_endpoint")]
        public string TokenEndpoint { get; set; }

        [JsonProperty("introspection_endpoint")]
        public string IntrospectionEndpoint { get; set; }

        [JsonProperty("revocation_endpoint")]
        public string RevocationEndpoint { get; set; }

        [JsonProperty("userinfo_endpoint")]
        public string UserInfoEndpoint { get; set; }

        [JsonProperty("registration_endpoint")]
        public string RegistrationEndpoint { get; set; }

        [JsonProperty("jwks_uri")]
        public string JwksUri { get; set; }

        [JsonProperty("scopes_supported")]
        public string[] ScopesSupported { get; set; }

        [JsonProperty("response_types_supported")]
        public string[] ResponseTypesSupported { get; set; }

        [JsonProperty("response_modes_supported")]
        public string[] ResponseModesSupported { get; set; }

        [JsonProperty("grant_types_supported")]
        public string[] GrantTypesSupported { get; set; }

        [JsonProperty("acr_values_supported")]
        public string[] AcrValuesSupported { get; set; }

        [JsonProperty("subject_types_supported")]
        public string[] SubjectTypesSupported { get; set; }

        [JsonProperty("id_token_signing_alg_values_supported")]
        public string[] IdTokenSigningAlgValuesSupported { get; set; }

        [JsonProperty("id_token_encryption_alg_values_supported")]
        public string[] IdTokenEncryptionAlgValuesSupported { get; set; }

        [JsonProperty("id_token_encryption_enc_values_supported")]
        public string[] IdTokenEncryptionEncValuesSupported { get; set; }

        [JsonProperty("cdr_arrangement_revocation_endpoint")]
        public string CdrArrangementRevocationEndpoint { get; set; }

        [JsonProperty("pushed_authorization_request_endpoint")]
        public string PushedAuthorizationRequestEndpoint { get; set; }

        [JsonProperty("request_object_signing_alg_values_supported")]
        public string[] RequestObjectSigningAlgValuesSupported { get; set; }

        [JsonProperty("token_endpoint_auth_methods_supported")]
        public string[] TokenEndpointAuthMethodsSupported { get; set; }

        [JsonProperty("tls_client_certificate_bound_access_tokens")]
        public bool TlsClientCertificateBoundAccessTokens { get; set; }

        [JsonProperty("claims_supported")]
        public string[] ClaimsSupported { get; set; }

        [JsonProperty("mtls_endpoint_aliases")]
        public MtlsAliases MtlsEndpointAliases { get; set; }

        public class MtlsAliases {
            [JsonProperty("token_endpoint")]
            public string TokenEndpoint { get; set; }

            [JsonProperty("revocation_endpoint")]
            public string RevocationEndpoint { get; set; } 

            [JsonProperty("introspection_endpoint")]
            public string IntrospectionEndpoint { get; set; } 
            
            [JsonProperty("pushed_authorization_request_endpoint")]
            public string PushedAuthorizationRequestEndpoint { get; set; } 
        }

        public string PreferentialTokenEndpoint => MtlsEndpointAliases?.TokenEndpoint ?? TokenEndpoint;

        public string PreferentialRevocationEndpoint => MtlsEndpointAliases?.RevocationEndpoint ?? RevocationEndpoint;

        public string PreferentialIntrospectionEndpoint => MtlsEndpointAliases?.IntrospectionEndpoint ?? IntrospectionEndpoint;

        public string PreferentialPushedAuthorizationRequestEndpoint => MtlsEndpointAliases?.PushedAuthorizationRequestEndpoint ?? PushedAuthorizationRequestEndpoint;
    }
}
