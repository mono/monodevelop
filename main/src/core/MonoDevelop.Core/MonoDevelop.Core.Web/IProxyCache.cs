using System;
using System.Net;

namespace MonoDevelop.Core.Web
{
	public interface IProxyCache
	{
		void Add (IWebProxy proxy);

		IWebProxy GetProxy (Uri uri);
	}
}
