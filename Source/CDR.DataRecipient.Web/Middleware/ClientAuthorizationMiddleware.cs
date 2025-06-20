using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CDR.DataRecipient.Web.Middleware
{
    public class ClientAuthorizationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IDataHolderDiscoveryCache _dataHolderDiscoveryCache;
        private readonly ILogger<ClientAuthorizationMiddleware> _logger;
        private readonly SoftwareProduct _softwareProduct;

        // Any path that requires a client authorisaton should be listed here.
        private readonly string[] _validPaths = new string[]
        {
            $"/{Common.Constants.Urls.ClientArrangementRevokeUrl}",
        };

        public ClientAuthorizationMiddleware(
            RequestDelegate next,
            IConfiguration config,
            IDataHolderDiscoveryCache dataHolderDiscoveryCache,
            ILogger<ClientAuthorizationMiddleware> logger)
        {
            this._next = next;
            this._dataHolderDiscoveryCache = dataHolderDiscoveryCache;
            this._logger = logger;
            this._softwareProduct = config.GetSoftwareProductConfig();
        }

        public async Task Invoke(HttpContext context)
        {
            // Check if the path required client authentication.
            if (this._validPaths.Contains(context.Request.Path.Value))
            {
                var authorisationHeader = context.Request.Headers.Authorization.FirstOrDefault();
                string token = null;
                if (authorisationHeader != null)
                {
                    var authorisationData = authorisationHeader.Split(" ");
                    token = authorisationData.Length > 0 ? authorisationData[^1] : null;
                }

                this._logger.LogDebug("Validating authorization token: {Token}", token);

                if (token != null)
                {
                    var claimsPrincipal = await this.ValidateTokenAsync(token);
                    if (claimsPrincipal != null)
                    {
                        context.Items[ClientAuthorizeAttribute.ClaimsPrincipalKey] = claimsPrincipal;
                    }
                }
            }

            await this._next(context);
        }

        public async Task<ClaimsPrincipal> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenJwt = token.GetJwt();
                this._logger.LogDebug("tokenJwt: {TokenJwt}", tokenJwt);

                // sub and the iss should be the same
                if (tokenJwt == null || tokenJwt.Issuer != tokenJwt.Subject)
                {
                    this._logger.LogError("Error in {MethodName}: iss and sub should be the same value.", nameof(this.ValidateTokenAsync));
                    return null;
                }

                // Get the data holder details
                var dataholderDiscoveryDocument = await this._dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(tokenJwt.Issuer);
                if (dataholderDiscoveryDocument == null)
                {
                    // There is no valid brand id in our DB for this issuer.
                    this._logger.LogError("Error in {MethodName}: could not find data holder discovery document.", nameof(this.ValidateTokenAsync));
                    return null;
                }

                this._logger.LogDebug("Validating token against {JwksUri}.", dataholderDiscoveryDocument.JwksUri);

                // Validate the token
                var validated = await token.ValidateToken(
                    dataholderDiscoveryDocument.JwksUri,
                    this._logger,
                    tokenJwt.Issuer,
                    new[] { this._softwareProduct.RevocationUri, this._softwareProduct.RecipientBaseUri },
                    validateLifetime: false);

                this._logger.LogDebug("Validated token: {IsValid}.", validated.IsValid);

                return validated.ClaimsPrincipal;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Client Authorisation Bearer token validation failed.");
                return null;
            }
        }
    }
}
