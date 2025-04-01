using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models
{
    public class OidcDiscovery
    {
        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }

        private string _tokenEndpoint;

        [JsonProperty("token_endpoint")]
        public string TokenEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(MtlsEndpointAliases?.TokenEndpoint))
                {
                    return MtlsEndpointAliases.TokenEndpoint;
                }

                return _tokenEndpoint;
            }

            set
            {
                _tokenEndpoint = value;
            }
        }

        private string _introspectionEndpoint;

        [JsonProperty("introspection_endpoint")]
        public string IntrospectionEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(MtlsEndpointAliases?.IntrospectionEndpoint))
                {
                    return MtlsEndpointAliases.IntrospectionEndpoint;
                }

                return _introspectionEndpoint;
            }

            set
            {
                _introspectionEndpoint = value;
            }
        }

        private string _revocationEndpoint;

        [JsonProperty("revocation_endpoint")]
        public string RevocationEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(MtlsEndpointAliases?.RevocationEndpoint))
                {
                    return MtlsEndpointAliases.RevocationEndpoint;
                }

                return _revocationEndpoint;
            }

            set => _revocationEndpoint = value;
        }

        private string _userInfoEndpoint;

        [JsonProperty("userinfo_endpoint")]
        public string UserInfoEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(MtlsEndpointAliases?.UserInfoEndpoint))
                {
                    return MtlsEndpointAliases.UserInfoEndpoint;
                }

                return _userInfoEndpoint;
            }

            set
            {
                _userInfoEndpoint = value;
            }
        }

        private string _registrationEndpoint;

        [JsonProperty("registration_endpoint")]
        public string RegistrationEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(MtlsEndpointAliases?.RegistrationEndpoint))
                {
                    return MtlsEndpointAliases.RegistrationEndpoint;
                }

                return _registrationEndpoint;
            }

            set
            {
                _registrationEndpoint = value;
            }
        }

        private string _pushedAuthorizationRequestEndpoint;

        [JsonProperty("pushed_authorization_request_endpoint")]
        public string PushedAuthorizationRequestEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(MtlsEndpointAliases?.PushedAuthorizationRequestEndpoint))
                {
                    return MtlsEndpointAliases.PushedAuthorizationRequestEndpoint;
                }

                return _pushedAuthorizationRequestEndpoint;
            }

            set
            {
                _pushedAuthorizationRequestEndpoint = value;
            }
        }

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

        [JsonProperty("cdr_arrangement_revocation_endpoint")]
        public string CdrArrangementRevocationEndpoint { get; set; }

        [JsonProperty("request_object_signing_alg_values_supported")]
        public string[] RequestObjectSigningAlgValuesSupported { get; set; }

        [JsonProperty("token_endpoint_auth_methods_supported")]
        public string[] TokenEndpointAuthMethodsSupported { get; set; }

        [JsonProperty("tls_client_certificate_bound_access_tokens")]
        public bool TlsClientCertificateBoundAccessTokens { get; set; }

        [JsonProperty("authorization_signing_alg_values_supported")]
        public string[] AuthorizationSigningResponseAlgValuesSupported { get; set; }

        [JsonProperty("authorization_encryption_enc_values_supported")]
        public string[] AuthorizationEncryptionResponseEncValuesSupported { get; set; }

        [JsonProperty("authorization_encryption_alg_values_supported")]
        public string[] AuthorizationEncryptionResponseAlgValuesSupported { get; set; }

        [JsonProperty("claims_supported")]
        public string[] ClaimsSupported { get; set; }

        [JsonProperty("mtls_endpoint_aliases")]
        public MtlsAliases MtlsEndpointAliases { get; set; }
    }

    public class MtlsAliases
    {
        [JsonProperty("token_endpoint")]
        public string TokenEndpoint { get; set; }

        [JsonProperty("revocation_endpoint")]
        public string RevocationEndpoint { get; set; }

        [JsonProperty("introspection_endpoint")]
        public string IntrospectionEndpoint { get; set; }

        [JsonProperty("userinfo_endpoint")]
        public string UserInfoEndpoint { get; set; }

        [JsonProperty("registration_endpoint")]
        public string RegistrationEndpoint { get; set; }

        [JsonProperty("pushed_authorization_request_endpoint")]
        public string PushedAuthorizationRequestEndpoint { get; set; }
    }
}
