using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xero.NetStandard.OAuth2.Config;
using Xero.NetStandard.OAuth2.Token;

namespace XeroNetStandardApp.Ledgerflow
{
	/// <summary>
	/// Utility class for validating JWTs
	/// </summary>
	/// <remarks>Xero's JwtUtils has a hard-coded test that the issuer is "https://identity.xero.com",
	/// which is not applicable to in the case of authenticating with Ledgerflow.
	/// This class does the same sort of validation, but without hard-coded issuer value check - 
	/// checking that it is signed by an authorized signatory is sufficient.
	/// </remarks>
	public static class TokenValidator
	{
		/// <summary>
		/// Test IdToken and AccessToken to see that they are valid.
		/// </summary>
		public static async Task<TokenValidation> GetTokenValidationAsync(XeroOAuth2Token token, XeroConfiguration xeroConfig)
		{
			var signingKeys = await GetSigningKeysAsync(xeroConfig.XeroIdentityBaseUri);
			var tokenHandler = new JwtSecurityTokenHandler();
			try
			{
				tokenHandler.ValidateToken(token.IdToken, GetTokenValidationParameters(signingKeys, xeroConfig.ClientId), out SecurityToken validatedToken);
			}
			catch (SecurityTokenValidationException)
			{
				return new TokenValidation(isValid: false, "ID token is not valid.");
			}

			try
			{
				tokenHandler.ValidateToken(token.AccessToken, GetTokenValidationParameters(signingKeys, null), out SecurityToken validatedToken);
			}
			catch (SecurityTokenValidationException)
			{
				return new TokenValidation(isValid: false, "Access token is not valid.");
			}

			return new TokenValidation(isValid: true);
		}

		private static async Task<ICollection<JsonWebKey>> GetSigningKeysAsync(string identityBaseUri)
		{
			// Ledgerflow supports .well-known/oauth-authorization-server, but Xero does not.
			// So for full compatibility in this code, we go straight to the jwks endpoint.

			string jwksUrl = getUrl(identityBaseUri, ".well-known/openid-configuration/jwks");
			using (var httpClient = new HttpClient())
			{
				JwkList jwks = await httpClient.GetFromJsonAsync<JwkList>(jwksUrl);
				return jwks.keys;
			}
		}

		private static TokenValidationParameters GetTokenValidationParameters(ICollection<JsonWebKey> signingKeys, string audience)
		{
			return new TokenValidationParameters
			{
				RequireExpirationTime = true,
				RequireSignedTokens = true,
				ValidateIssuer = false,
				ValidateIssuerSigningKey = true,
				IssuerSigningKeys = signingKeys,
				ValidateAudience = audience != null,
				ValidAudience = audience,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.FromMinutes(2)
			};
		}

		private static string getUrl(string identityUrlBase, string path)
		{
			var uriBuilder = new UriBuilder(identityUrlBase) { Path = path };
			return uriBuilder.ToString();
		}

		private class JwkList
		{
			public List<JsonWebKey> keys { get; set; }
		}
	}
}
