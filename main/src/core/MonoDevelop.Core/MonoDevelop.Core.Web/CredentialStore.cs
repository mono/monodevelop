using System;
using System.Collections.Concurrent;
using System.Net;

namespace MonoDevelop.Core.Web
{
	class CredentialStore : ICredentialCache
	{
		readonly ConcurrentDictionary<Uri, ICredentials> credentialCache = new ConcurrentDictionary<Uri, ICredentials> ();
		static readonly CredentialStore instance = new CredentialStore ();

		public static CredentialStore Instance {
			get {
				return instance;
			}
		}

		public ICredentials GetCredentials (Uri proxy, Uri uri)
		{
			Uri rootUri = GetRootUri (uri);

			// Check our cache first
			ICredentials credentials;
			if (credentialCache.TryGetValue (uri, out credentials) ||
				credentialCache.TryGetValue (rootUri, out credentials)) {
				return credentials;
			}

			// Then go to the keychain
			var creds = PasswordService.GetWebUserNameAndPassword (proxy);

			return creds != null ? new NetworkCredential(creds.Item1, creds.Item2).AsCredentialCache (uri) : null;
		}

		static readonly string[] AuthenticationSchemes = { "Basic", "NTLM", "Negotiate" };

		public void Add (Uri requestUri, Uri proxy, ICredentials credentials)
		{
			Uri rootUri = GetRootUri (requestUri);
			credentialCache.TryAdd (requestUri, credentials);
			credentialCache.AddOrUpdate (rootUri, credentials, (u, c) => credentials);

			NetworkCredential cred = null;
			foreach (var scheme in AuthenticationSchemes) {
				cred = credentials.GetCredential (requestUri, scheme);
				if (cred != null)
					break;
			}

			if (cred != null)
				PasswordService.AddWebUserNameAndPassword (proxy, cred.UserName, cred.Password);
		}

		static Uri GetRootUri (Uri uri)
		{
			return new Uri (uri.GetComponents (UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
		}
	}
}
