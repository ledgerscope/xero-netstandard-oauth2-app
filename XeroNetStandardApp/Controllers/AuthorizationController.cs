using Microsoft.AspNetCore.Mvc;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Config;
using Xero.NetStandard.OAuth2.Token;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using System.Text.Json;
using XeroNetStandardApp.Ledgerflow;

namespace XeroNetStandardApp.Controllers
{
    /// <summary>
    /// Authorize organisation with sample app
    /// </summary>
    public class AuthorizationController : BaseXeroOAuth2Controller
    {
        private const string StateFilePath = "./state.json";

        private readonly XeroClient _client;

        public AuthorizationController(IOptions<XeroConfiguration> xeroConfig) : base(xeroConfig)
        {
            _client = new XeroClient(xeroConfig.Value);
        }

        /// <summary>
        /// Generate random guid for site security
        /// </summary>
        /// <returns></returns>
        public IActionResult Index(AccountingServiceProvider provider)
        {
            var clientState = Guid.NewGuid().ToString();
            StoreState(clientState);

            return Redirect(LedgerflowUtilities.GetLedgerflowLoginUrlForProvider(this.xeroConfig.Value, provider, clientState));
        }

        // GET /Authorization/Callback
        /// <summary>
        /// Callback validating returned data to prevent cross site forgey attacks
        /// </summary>
        /// <param name="code">Returned code</param>
        /// <param name="state">Returned state</param>
        /// <returns>Redirect to organisations page</returns>
        public async Task<IActionResult> Callback(string code, string state)
        {
            var clientState = GetCurrentState();
            if (state != clientState)
            {
                return Content("Cross site forgery attack detected!");
            }

            var xeroToken = (XeroOAuth2Token) await _client.RequestAccessTokenAsync(code);

            var tokenValidation = await TokenValidator.GetTokenValidationAsync(xeroToken, this.xeroConfig.Value);
            if (tokenValidation.IsValid)
            {
                tokenIO.StoreToken(xeroToken);
                return RedirectToAction("Index", "OrganisationInfo");
            }
            else
            {
                return Content(tokenValidation.Message);
            }
		}

        /// <summary>
        /// Disconnect org connections to sample app. Destroys token
        /// <para>GET /Authorization/Disconnect</para>
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Disconnect()
        {
            await _client.DeleteConnectionAsync(XeroToken, XeroToken.Tenants[0]);
            tokenIO.DestroyToken();

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Revoke Xero OAuth2 token
        ///<para>GET: /Authorization/Revoke</para>
        /// </summary>
        /// <returns>Redirect to home page</returns>
        public async Task<IActionResult> Revoke()
        {
            await _client.RevokeAccessTokenAsync(XeroToken);
            tokenIO.DestroyToken();

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Save state value to disk
        /// </summary>
        /// <param name="state">State data to save</param>
        private void StoreState(string state)
        {
            var serializedState = JsonSerializer.Serialize(new State{state = state});
            System.IO.File.WriteAllText(StateFilePath, serializedState);
        }

        /// <summary>
        /// Get current state from disk
        /// </summary>
        /// <returns>Returns state from disk if exists, otherwise returns null</returns>
        private string GetCurrentState()
        {
            if (System.IO.File.Exists(StateFilePath))
            {
                var serializeState = System.IO.File.ReadAllText(StateFilePath);
                return JsonSerializer.Deserialize<State>(serializeState)?.state;
            }

            return null;
        }
    }

    /// <summary>
    /// Holds file structure for saving state to disk
    /// </summary>
    internal class State
    {
        public string state { get; set; }
    }
}
