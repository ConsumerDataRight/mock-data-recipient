using Newtonsoft.Json;

namespace CDR.DataRecipient.SDK.Models
{
    public class OidcDiscovery
    {
        private string _tokenEndpoint;
        private string _introspectionEndpoint;
        private string _revocationEndpoint;
        private string _userInfoEndpoint;
        private string _registrationEndpoint;
        private string _pushedAuthorizationRequestEndpoint;

        [JsonProperty("issuer")]
        public string Issuer { get; set; }

        [JsonProperty("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; }

        [JsonProperty("token_endpoint")]
        public string TokenEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(this.MtlsEndpointAliases?.TokenEndpoint))
                {
                    return this.MtlsEndpointAliases.TokenEndpoint;
                }

                return this._tokenEndpoint;
            }

            set
            {
                this._tokenEndpoint = value;
            }
        }

        [JsonProperty("introspection_endpoint")]
        public string IntrospectionEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(this.MtlsEndpointAliases?.IntrospectionEndpoint))
                {
                    return this.MtlsEndpointAliases.IntrospectionEndpoint;
                }

                return this._introspectionEndpoint;
            }

            set
            {
                this._introspectionEndpoint = value;
            }
        }

        [JsonProperty("revocation_endpoint")]
        public string RevocationEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(this.MtlsEndpointAliases?.RevocationEndpoint))
                {
                    return this.MtlsEndpointAliases.RevocationEndpoint;
                }

                return this._revocationEndpoint;
            }

            set => this._revocationEndpoint = value;
        }

        [JsonProperty("userinfo_endpoint")]
        public string UserInfoEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(this.MtlsEndpointAliases?.UserInfoEndpoint))
                {
                    return this.MtlsEndpointAliases.UserInfoEndpoint;
                }

                return this._userInfoEndpoint;
            }

            set
            {
                this._userInfoEndpoint = value;
            }
        }

        [JsonProperty("registration_endpoint")]
        public string RegistrationEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(this.MtlsEndpointAliases?.RegistrationEndpoint))
                {
                    return this.MtlsEndpointAliases.RegistrationEndpoint;
                }

                return this._registrationEndpoint;
            }

            set
            {
                this._registrationEndpoint = value;
            }
        }

        [JsonProperty("pushed_authorization_request_endpoint")]
        public string PushedAuthorizationRequestEndpoint
        {
            get
            {
                if (!string.IsNullOrEmpty(this.MtlsEndpointAliases?.PushedAuthorizationRequestEndpoint))
                {
                    return this.MtlsEndpointAliases.PushedAuthorizationRequestEndpoint;
                }

                return this._pushedAuthorizationRequestEndpoint;
            }

            set
            {
                this._pushedAuthorizationRequestEndpoint = value;
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
}
