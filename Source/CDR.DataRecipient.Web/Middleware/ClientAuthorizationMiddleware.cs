using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using CDR.DataRecipient.SDK.Extensions;
using CDR.DataRecipient.Web.Common;
using CDR.DataRecipient.Web.Configuration;
using CDR.DataRecipient.Web.Configuration.Models;
using CDR.DataRecipient.Web.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace CDR.DataRecipient.Web.Middleware
{
	public class ClientAuthorizationMiddleware
	{
		private readonly RequestDelegate _next;
		private readonly IConfiguration _config;
		private readonly IDataHolderDiscoveryCache _dataHolderDiscoveryCache;
		private readonly ILogger<ClientAuthorizationMiddleware> _logger;
		private readonly SoftwareProduct _softwareProduct;

		// Any path that requires a client authorisaton should be listed here.
		private readonly string[] _validPaths = new string[]
		{
			$"/{Constants.Urls.ClientArrangementRevokeUrl}"
		};

		public ClientAuthorizationMiddleware(RequestDelegate next,
			IConfiguration config,
			IDataHolderDiscoveryCache dataHolderDiscoveryCache,
			ILogger<ClientAuthorizationMiddleware> logger)
		{
			_next = next;
			_config = config;
			_dataHolderDiscoveryCache = dataHolderDiscoveryCache;
			_logger = logger;

			_softwareProduct = _config.GetSoftwareProductConfig();
		}

		public async Task Invoke(HttpContext context)
		{
			// Check if the path required client authentication.
			if (_validPaths.Contains(context.Request.Path.Value))
			{
				var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
				if (token != null)
				{
					var claimsPrincipal = await ValidateTokenAsync(token);
					if (claimsPrincipal != null)
					{
						context.Items[ClientAuthorize.ClaimsPrincipalKey] = claimsPrincipal;
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

				// sub and the iss should be the same
				if (tokenJwt == null || tokenJwt.Issuer != tokenJwt.Subject)
				{
					return null;
				}

				// Get the data holder details
				var dataholderDiscoveryDocument = await _dataHolderDiscoveryCache.GetOidcDiscoveryByBrandId(tokenJwt.Issuer);
				if (dataholderDiscoveryDocument == null)
				{
					// There is no valid brand id in our DB for this issuer.
					return null;
				}

				// Get the DH JWKS
				var jwks = await GetJwks(dataholderDiscoveryDocument.JwksUri);
				if (jwks == null || jwks.Keys.Count == 0)
				{
					return null;
				}

                // Validate the token
                var validationParameters = new TokenValidationParameters()
                {
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(2),

                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKeys = jwks.Keys,

                    ValidateAudience = true,
                    ValidAudiences = new[] { _softwareProduct.RevocationUri, _softwareProduct.RecipientBaseUri },

                    ValidateIssuer = true,
                    ValidIssuer = tokenJwt.Issuer,
                };
                return new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out var validatedToken);                
            }
			catch (Exception ex)
			{
				_logger.LogError(ex, "Client Authorisation Bearer token validation failed.");
				return null;
			}
		}

		private async Task<JsonWebKeySet> GetJwks(string jwksEndpoint)
		{
			var clientHandler = new HttpClientHandler()
			{
                AutomaticDecompression = DecompressionMethods.GZip
            };
			
			clientHandler.ServerCertificateCustomValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
			var jwksClient = new HttpClient(clientHandler);
			var jwksResponse = await jwksClient.GetAsync(jwksEndpoint);
			return new JsonWebKeySet(await jwksResponse.Content.ReadAsStringAsync());
		}
	}
}
