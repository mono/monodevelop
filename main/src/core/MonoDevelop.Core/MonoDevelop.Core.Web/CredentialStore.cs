using System;
using System.Collections.Concurrent;
using System.Net;

namespace MonoDevelop.Core.Web
{
	class CredentialStore : ICredentialCache
	{
		readonly ConcurrentDictionary<Uri, ICredentials> credentialCache = new ConcurrentDictionary<Uri, ICredentials> ();

		public ICredentials GetCredentials (Uri uri, CredentialType credentialType)
		{
			ICredentials credentials;
			if (!credentialCache.TryGetValue (uri, out credentials)) {
				if (credentialType == CredentialType.RequestCredentials &&
				    credentialCache.TryGetValue (GetRootUri (uri), out credentials))
					return credentials;
			} else {
				return credentials;
			}

			// Then go to the keychain
			var creds = PasswordService.GetWebUserNameAndPassword (uri);
			return creds != null ? new NetworkCredential (creds.Item1, creds.Item2).AsCredentialCache (uri) : null;
		}

		public void Add (Uri requestUri, ICredentials credentials, CredentialType credentialType)
		{
			credentialCache.TryAdd (requestUri, credentials);
			if (credentialType == CredentialType.RequestCredentials) {
				var rootUri = GetRootUri (requestUri);
				credentialCache.AddOrUpdate (rootUri, credentials, (u, c) => credentials);
			}

			var cred = Utility.GetCredentialsForUriFromICredentials (requestUri, credentials);
			if (cred != null && !string.IsNullOrWhiteSpace (cred.UserName) && !string.IsNullOrWhiteSpace (cred.Password))
				PasswordService.AddWebUserNameAndPassword (requestUri, cred.UserName, cred.Password);
		}

		static Uri GetRootUri (Uri uri)
		{
			return new Uri (uri.GetComponents (UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
		}
	}
}
