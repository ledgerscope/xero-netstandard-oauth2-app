using System;
using System.Text.Json;
using Xero.NetStandard.OAuth2.Api;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Config;

namespace XeroNetStandardApp.Ledgerflow
{
	public static class LedgerflowUtilities
	{
		/// <summary>
		/// Get OAuth initiation url for Ledgerflow, appropriate to the
		/// Accounting Software Provider that you want it to connect to.
		/// </summary>
		public static string GetLedgerflowLoginUrlForProvider(XeroConfiguration xeroConfig, AccountingServiceProvider provider, string state)
		{
			XeroClient client;
			var loginUri = new UriHandler(xeroConfig.XeroLoginBaseUri);
			if (loginUri.IsXeroHost)
			{
				if (provider == AccountingServiceProvider.NotSet || provider == AccountingServiceProvider.Xero)
				{
					// The Xero way.
					client = new XeroClient(xeroConfig);
				}
				else
				{
					throw new NotSupportedException($"xero.com API does not support connecting to {provider} sources.");
				}
			}
			else
			{
				// The Ledgerflow way, with added indication of which Accounting Service Provider
				// you intend to connect to on the far side of Ledgerflow.

				var configUri = new UriHandler(xeroConfig.XeroLoginBaseUri);
				configUri.SetPath($"/{(int)provider}{configUri.Path.TrimEnd('/')}");

				var config = GetConfigurationCopy(xeroConfig);
				config.XeroLoginBaseUri = configUri.ToString();

				client = new XeroClient(config);
			}

			return client.BuildLoginUri(state);
		}

		private static XeroConfiguration GetConfigurationCopy(XeroConfiguration copyMe)
		{
			string json = JsonSerializer.Serialize(copyMe);
			return JsonSerializer.Deserialize<XeroConfiguration>(json);
		}

		internal static T GetApi<T>(XeroConfiguration xeroConfig) where T : IApiAccessor, new()
		{
			var apiUri = new UriHandler(xeroConfig.XeroApiBaseUri);
			if (apiUri.IsXeroHost)
			{
				return new T();
			}
			else if (typeof(AccountingApi).IsAssignableFrom(typeof(T)))
			{
				apiUri.SetPath("api.xro/2.0");
				return (T)Activator.CreateInstance(typeof(T), apiUri.ToString());
			}
			else if (typeof(IdentityApi).IsAssignableFrom(typeof(T)))
			{
				return (T)Activator.CreateInstance(typeof(T), apiUri.ToString());
			}
			else
			{
				throw new NotImplementedException($"Ledgerflow supports {nameof(AccountingApi)}; it does not support {typeof(T).Name}.");
			}
		}

		private class UriHandler : UriBuilder
		{
			internal UriHandler(string uri)
				: base(uri) { }

			internal void SetPath(string path)
			{
				this.Path = path;
			}

			internal bool IsXeroHost
				=> this.Host.EndsWith(".xero.com", StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
