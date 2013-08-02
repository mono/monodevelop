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
using MonoDevelop.Components.MainToolbar;
using System.Net;

namespace MonoDevelop.MacIntegration
{
	public class MacProxyCredentialProvider : ICredentialProvider
	{
		string username, password;

		public ICredentials GetCredentials (Uri uri, IWebProxy proxy, CredentialType credentialType, ICredentials existingCredentials, bool retrying)
		{
			bool result = false;
			DispatchService.GuiSyncDispatch (() => {
				using (var ns = new NSAutoreleasePool ()) {
					var message = string.Format ("{0} needs {1} credentials to access {2}.", BrandingService.ApplicationName, 
					                             credentialType == CredentialType.ProxyCredentials ? "proxy" : "request", uri.Host);

					NSAlert alert = NSAlert.WithMessage ("Credentials Required", "OK", "Cancel", null, message);
					alert.Icon = NSApplication.SharedApplication.ApplicationIconImage;

					NSView view = new NSView (new RectangleF (0, 0, 313, 91));

					var creds = Utility.GetCredentialsForUriFromICredentials (uri, existingCredentials);

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
					usernameInput.StringValue = creds != null ? creds.UserName : string.Empty;
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
					passwordInput.StringValue = creds != null ? creds.Password : string.Empty;
					view.AddSubview (passwordInput);

					alert.AccessoryView = view;
					result = alert.RunModal () == 1;

					username = usernameInput.StringValue;
					password = passwordInput.StringValue;
				}
			});

			return result ? new NetworkCredential (username, password) : null;
		}
	}
}

