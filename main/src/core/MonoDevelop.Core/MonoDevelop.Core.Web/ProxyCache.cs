using System;
using System.Collections.Concurrent;
using System.Net;

namespace MonoDevelop.Core.Web
{
	class ProxyCache : IProxyCache
	{
		/// <summary>
		/// Capture the default System Proxy so that it can be re-used by the IProxyFinder
		/// because we can't rely on WebRequest.DefaultWebProxy since someone can modify the DefaultWebProxy
		/// property and we can't tell if it was modified and if we are still using System Proxy Settings or not.
		/// </summary>
		static readonly IWebProxy originalSystemProxy = WebRequest.GetSystemWebProxy ();

		readonly ConcurrentDictionary<Uri, WebProxy> cache = new ConcurrentDictionary<Uri, WebProxy> ();
		static readonly Lazy<ProxyCache> instance = new Lazy<ProxyCache> (() => new ProxyCache ());

		public static ProxyCache Instance {
			get {
				return instance.Value;
			}
		}

		ProxyCache () {}

		public IWebProxy GetProxy (Uri uri)
		{
			if (!IsSystemProxySet (uri))
				return null;

			WebProxy systemProxy = GetSystemProxy (uri), effectiveProxy;

			// See if we have a proxy instance cached for this proxy address
			return cache.TryGetValue (systemProxy.Address, out effectiveProxy) ? effectiveProxy : systemProxy;

		}

		public void Add (IWebProxy proxy)
		{
			var webProxy = proxy as WebProxy;
			if (webProxy != null)
				cache.TryAdd (webProxy.Address, webProxy);
		}

		static WebProxy GetSystemProxy (Uri uri)
		{
			// WebRequest.DefaultWebProxy seems to be more capable in terms of getting the default
			// proxy settings instead of the WebRequest.GetSystemProxy()
			var proxyUri = originalSystemProxy.GetProxy (uri);
			return new WebProxy (proxyUri);
		}

		/// <summary>
		/// Return true or false if connecting through a proxy server
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		static bool IsSystemProxySet (Uri uri)
		{
			// The reason for not calling the GetSystemProxy is because the object
			// that will be returned is no longer going to be the proxy that is set by the settings
			// on the users machine only the Address is going to be the same.
			// Not sure why the .NET team did not want to expose all of the useful settings like
			// ByPass list and other settings that we can't get because of it.
			// Anyway the reason why we need the DefaultWebProxy is to see if the uri that we are
			// getting the proxy for to should be bypassed or not. If it should be bypassed then
			// return that we don't need a proxy and we should try to connect directly.
			IWebProxy proxy = WebRequest.DefaultWebProxy;
			if (proxy != null) {
				Uri proxyAddress = new Uri (proxy.GetProxy (uri).AbsoluteUri);
				if (String.Equals (proxyAddress.AbsoluteUri, uri.AbsoluteUri))
					return false;
				if (proxy.IsBypassed (uri))
					return false;
				proxy = new WebProxy (proxyAddress);
			}

			return proxy != null;
		}
	}
}
