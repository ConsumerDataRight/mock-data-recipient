using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.SDK.Models;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Extensions;
using CDR.DataRecipient.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CDR.DataRecipient.Web.Middleware
{
	public class ClientAuthorizationMiddleware
	{
		private readonly IConfiguration _config;
		private readonly RequestDelegate _next;
		private readonly IDataHolderDiscoveryCache _dataHolderDiscoveryCache;
		private readonly ILogger<ClientAuthorizationMiddleware> _logger;
		private readonly SoftwareProduct _softwareProduct;

		// Any path that requires a client authorisaton should be listed here.
		private readonly string[] _validPaths = new string[]
		{
			$"/{Common.Constants.Urls.ClientArrangementRevokeUrl}"
		};

		public ClientAuthorizationMiddleware(RequestDelegate next,
			IConfiguration config,
			IDataHolderDiscoveryCache dataHolderDiscoveryCache,
			ILogger<ClientAuthorizationMiddleware> logger)
		{
			_next = next;
			_dataHolderDiscoveryCache = dataHolderDiscoveryCache;
			_logger = logger;
			_config = config;
			_softwareProduct = _config.GetSoftwareProductConfig();
		}

		public async Task Invoke(HttpContext context)
		{
			// Check if the path required client authentication.
			if (_validPaths.Contains(context.Request.Path.Value))
			{
				var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

				_logger.LogDebug("Validating authorization token: {token}", token);

				if (token != null)
				{
					var claimsPrincipal = await ValidateTokenAsync(token);
					if (claimsPrincipal != null)
					{
						context.Items[ClientAuthorizeAttribute.ClaimsPrincipalKey] = claimsPrincipal;
					}
				}
			}

			await _next(context);
		}

		public async Task<ClaimsPrincipal> ValidateTokenAsync(string token)
		{
			try
			{
				var tokenJwt = token.GetJwt();
				_logger.LogDebug("tokenJwt: {tokenJwt}", tokenJwt);

				// sub and the iss should be the same
				if (tokenJwt == null || tokenJwt.Issuer != tokenJwt.Subject)
				{
					_logger.LogError("Error in {methodName}: iss and sub should be the same value.", nameof(ValidateTokenAsync));
					return null;
				}

				// Get the data holder details
				var dataholderDiscoveryDocument = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(tokenJwt.Issuer);
				if (dataholderDiscoveryDocument == null)
				{
					// There is no valid brand id in our DB for this issuer.
					_logger.LogError("Error in {methodName}: could not find data holder discovery document.", nameof(ValidateTokenAsync));
					return null;
				}

				_logger.LogDebug("Validating token against {jwksUri}.", dataholderDiscoveryDocument.JwksUri);

				// Validate the token
				var validated = await token.ValidateToken(
					dataholderDiscoveryDocument.JwksUri,
					_logger,
					tokenJwt.Issuer,
					new[] { _softwareProduct.RevocationUri, _softwareProduct.RecipientBaseUri },
					validateLifetime: false);

				_logger.LogDebug("Validated token: {isValid}.", validated.IsValid);

				return validated.ClaimsPrincipal;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Client Authorisation Bearer token validation failed.");
				return null;
			}
		}
	}
}