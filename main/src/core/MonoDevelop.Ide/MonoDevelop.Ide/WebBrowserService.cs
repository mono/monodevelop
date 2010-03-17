// 
// WebBrowserService.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;

using Mono.Addins;
using MonoDevelop.Ide.WebBrowser;

namespace MonoDevelop.Ide
{
	public static class WebBrowserService
	{
		readonly static string browserPath = "/MonoDevelop/Core/WebBrowsers";
		static List<IWebBrowserLoader> loaders = new List<IWebBrowserLoader> ();
		
		static WebBrowserService ()
		{
			AddinManager.AddExtensionNodeHandler (browserPath, new ExtensionNodeEventHandler (extensionHandler));
		}
		
		static void extensionHandler (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add)
				loaders.Add ((IWebBrowserLoader) args.ExtensionObject);
			else if (args.Change == ExtensionChange.Remove)
				loaders.Remove ((IWebBrowserLoader) args.ExtensionObject);
		}
		
		public static IWebBrowser GetWebBrowser ()
		{
			foreach (IWebBrowserLoader loader in loaders)
				if (loader.CanCreateBrowser)
					return loader.GetBrowser ();
			throw new InvalidOperationException ("Was not able to create web browser; either the consumer did not check for browser availability, or the extension tree has changed since this check.");
		}
		
		public static bool CanGetWebBrowser {
			get {
				foreach (IWebBrowserLoader loader in loaders)
					if (loader.CanCreateBrowser)
						return true;
				return false;
			}
		}
	}
}
