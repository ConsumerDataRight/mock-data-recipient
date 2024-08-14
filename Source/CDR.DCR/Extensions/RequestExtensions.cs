using CDR.DCR.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace CDR.DCR.Extensions
{
    public static class RequestExtensions
    {
        public static (List<Claim>, string) CreateClaimsForDCRRequest(this DcrRequest dcrRequest)
        {
            string errorMessage = string.Empty;

             // Error - Unable to perform DCR as there are no mutually supported values in the mandatory claim [CLAIM_NAME]
            const string ErrorMessage = "Unable to perform DCR as there are no mutually supported values in the mandatory claim";

            var claims = new List<Claim>
            {
                new("jti", Guid.NewGuid().ToString()),
                new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer),
                new("token_endpoint_auth_signing_alg", "PS256"),
                new("token_endpoint_auth_method", "private_key_jwt"),
                new("application_type", "web"),
                new("id_token_signed_response_alg", "PS256"),
                new("id_token_encrypted_response_alg", "RSA-OAEP"),
                new("id_token_encrypted_response_enc", "A256GCM"),
                new("request_object_signing_alg", "PS256"),
                new("software_statement", dcrRequest.Ssa ?? ""),
                new("grant_types", "client_credentials"),
                new("grant_types", "authorization_code"),
                new("grant_types", "refresh_token")
            };

            // response_types updated below "code, code id_token" both types are returned and added below
            // A response type is mandatory
            if (!dcrRequest.ResponseTypesSupported.Contains("code") && !dcrRequest.ResponseTypesSupported.Contains("code id_token"))
            {
                // Return the error                 
                errorMessage = ErrorMessage + " response_types";
                return (null, errorMessage);
            }

            var responseTypesList = dcrRequest.ResponseTypesSupported.Where(x => x.ToLower().Equals("code") || x.ToLower().Equals("code id_token")).ToList();
            claims.Add(new Claim("response_types", JsonConvert.SerializeObject(responseTypesList), JsonClaimValueTypes.JsonArray));


            var isCodeFlow = dcrRequest.ResponseTypesSupported.Contains("code");
            if (isCodeFlow && dcrRequest.AuthorizationSigningResponseAlgValuesSupported.Length==0)
            {
                // Log error message to the mandatory claim missing
                errorMessage = ErrorMessage + " authorization_signed_response_alg";
                return (null, errorMessage);
            }

            // Mandatory for code flow
            if (isCodeFlow)
            {
                if (!dcrRequest.AuthorizationSigningResponseAlgValuesSupported.Contains("PS256") && !dcrRequest.AuthorizationSigningResponseAlgValuesSupported.Contains("ES256"))
                {
                    // Return the error
                    errorMessage = ErrorMessage + " authorization_signed_response_alg";
                    return (null, errorMessage);
                }

                if (dcrRequest.AuthorizationSigningResponseAlgValuesSupported.Contains("PS256"))
                {
                    claims.Add(new Claim("authorization_signed_response_alg", "PS256"));
                }
                else if (dcrRequest.AuthorizationSigningResponseAlgValuesSupported.Contains("ES256"))
                {
                    claims.Add(new Claim("authorization_signed_response_alg", "ES256"));
                }
            }

            // Check if the enc is empty but a alg is specified.
            if ((dcrRequest.AuthorizationEncryptionResponseEncValuesSupported == null || dcrRequest.AuthorizationEncryptionResponseEncValuesSupported.Length==0) // No enc specified
                && dcrRequest.AuthorizationEncryptionResponseAlgValuesSupported != null && dcrRequest.AuthorizationEncryptionResponseAlgValuesSupported.Contains("RSA-OAEP-256")
                && dcrRequest.AuthorizationEncryptionResponseAlgValuesSupported.Contains("RSA-OAEP")) // but alg specified.
            {
                errorMessage = ErrorMessage + " authorization_encrypted_response_enc";
                return (null, errorMessage);
            }


            if (dcrRequest.AuthorizationEncryptionResponseEncValuesSupported != null && dcrRequest.AuthorizationEncryptionResponseEncValuesSupported.Contains("A128CBC-HS256"))
            {
                claims.Add(new Claim("authorization_encrypted_response_enc", "A128CBC-HS256"));
            }
            else if (dcrRequest.AuthorizationEncryptionResponseEncValuesSupported != null && dcrRequest.AuthorizationEncryptionResponseEncValuesSupported.Contains("A256GCM"))
            {
                claims.Add(new Claim("authorization_encrypted_response_enc", "A256GCM"));
            }

            // Conditional: Optional for response_type "code" if authorization_encryption_enc_values_supported is present            
            if (isCodeFlow && dcrRequest.AuthorizationEncryptionResponseAlgValuesSupported != null && dcrRequest.AuthorizationEncryptionResponseAlgValuesSupported.Length!=0)
            {
                if (dcrRequest.AuthorizationEncryptionResponseAlgValuesSupported.Contains("RSA-OAEP-256"))
                {
                    claims.Add(new Claim("authorization_encrypted_response_alg", "RSA-OAEP-256"));
                }
                else if (dcrRequest.AuthorizationEncryptionResponseAlgValuesSupported.Contains("RSA-OAEP"))
                {
                    claims.Add(new Claim("authorization_encrypted_response_alg", "RSA-OAEP"));
                }
            }

            char[] delimiters = [',', ' '];
            var redirectUrisList = dcrRequest.RedirectUris?.Split(delimiters).ToList();
            claims.Add(new Claim("redirect_uris", JsonConvert.SerializeObject(redirectUrisList), JsonClaimValueTypes.JsonArray));

            return (claims, errorMessage);
        }
    }
}
