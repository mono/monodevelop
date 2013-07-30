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

		public ICredentials GetCredentials (Uri uri)
		{
			Uri rootUri = GetRootUri (uri);

			ICredentials credentials;
			if (credentialCache.TryGetValue (uri, out credentials) ||
				credentialCache.TryGetValue (rootUri, out credentials)) {
				return credentials;
			}

			return null;
		}

		public void Add (Uri uri, ICredentials credentials)
		{
			Uri rootUri = GetRootUri (uri);
			credentialCache.TryAdd (uri, credentials);
			credentialCache.AddOrUpdate (rootUri, credentials, (u, c) => credentials);
		}

		internal static Uri GetRootUri (Uri uri)
		{
			return new Uri (uri.GetComponents (UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
		}
	}
}
