//
// MacProxyCredentialsProvider.cs
//
// Author:
//       Bojan Rajkovic <bojan.rajkovic@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Drawing;

using MonoDevelop.Core;
using MonoDevelop.Core.Web;
using MonoDevelop.Ide;

using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Net;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MacIntegration
{
	class MacProxyCredentialProvider : ICredentialProvider
	{
		public ICredentials GetCredentials (Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
		{
			var rootUri = new Uri (uri.GetComponents (UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));

			if (!retrying) {
				// try to find stored credentials in the keychain and return those
				// if they fail, we'll be called again with retrying = true
				var existing =
					Keychain.FindInternetUserNameAndPassword (uri) ??
					Keychain.FindInternetUserNameAndPassword (rootUri);
				if (existing != null)
					return new NetworkCredential (existing.Item1, existing.Item2);
			}

			ICredentials result = null;

			DispatchService.GuiSyncDispatch (() => {

				using (var ns = new NSAutoreleasePool ()) {
					var message = credentialType == CredentialType.ProxyCredentials
						? GettextCatalog.GetString (
							"{0} needs proxy credentials to access {1}.",
							BrandingService.ApplicationName,
							uri.Host
						)
						: GettextCatalog.GetString (
							"{0} needs web credentials to access {1}.",
							BrandingService.ApplicationName,
							uri.Host
						);

					var alert = NSAlert.WithMessage (
						GettextCatalog.GetString ("Credentials Required"),
						GettextCatalog.GetString ("OK"),
						GettextCatalog.GetString ("Cancel"),
						null,
						message
					);

					alert.Icon = NSApplication.SharedApplication.ApplicationIconImage;

					var view = new NSView (new RectangleF (0, 0, 313, 91));

					var usernameLabel = new NSTextField (new RectangleF (17, 55, 71, 17)) {
						Identifier = "usernameLabel",
						StringValue = "Username:",
						Alignment = NSTextAlignment.Right,
						Editable = false,
						Bordered = false,
						DrawsBackground = false,
						Bezeled = false,
						Selectable = false,
					};
					view.AddSubview (usernameLabel);

					var usernameInput = new NSTextField (new RectangleF (93, 52, 200, 22));
					view.AddSubview (usernameInput);

					var passwordLabel = new NSTextField (new RectangleF (22, 23, 66, 17)) {
						StringValue = "Password:",
						Alignment = NSTextAlignment.Right,
						Editable = false,
						Bordered = false,
						DrawsBackground = false,
						Bezeled = false,
						Selectable = false,
					};
					view.AddSubview (passwordLabel);

					var passwordInput = new NSSecureTextField (new RectangleF (93, 20, 200, 22));
					view.AddSubview (passwordInput);

					alert.AccessoryView = view;

					if (alert.RunModal () != 1)
						return;

					var username = usernameInput.StringValue;
					var password = passwordInput.StringValue;
					result = new NetworkCredential (username, password);

					// store the obtained credentials in the keychain
					// but don't store for the root url since it may have other credentials
					Keychain.AddInternetPassword (uri, username, password);
				}
			});

			return result;
		}
	}
}

