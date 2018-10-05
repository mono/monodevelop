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
using System.Net;

using MonoDevelop.Core;
using MonoDevelop.Core.Web;
using MonoDevelop.Ide;

using AppKit;
using CoreGraphics;
using Foundation;
using MonoDevelop.MacInterop;

namespace MonoDevelop.MacIntegration
{
	class MacProxyCredentialProvider : ICredentialProvider
	{
		static nint NSAlertFirstButtonReturn = 1000;
		object guiLock = new object();

		public ICredentials GetCredentials (Uri uri, IWebProxy proxy, CredentialType credentialType, bool retrying)
		{
			// if looking for proxy credentials, we care about the proxy's URL, not the request URL
			if (credentialType == CredentialType.ProxyCredentials) {
				var proxyUri = proxy.GetProxy (uri);
				if (proxyUri != null)
					uri = proxyUri;
			}

			lock (guiLock) {
				// If this is the first attempt, return any stored credentials. If they fail, we'll be called again.
				if (!retrying) {
					var creds = GetExistingCredentials (uri, credentialType);
					if (creds != null)
						return creds;
				}

				return GetCredentialsFromUser (uri, proxy, credentialType);
			}
		}

		static ICredentials GetSystemProxyCredentials (Uri uri)
		{
			var kind = SecProtocolType.Any;
			if (uri.Scheme == "http")
				kind = SecProtocolType.HTTPProxy;
			else if (uri.Scheme == "https")
				kind = SecProtocolType.HTTPSProxy;

			//TODO: get username from SystemConfiguration APIs so we don't trigger a double auth prompt
			var existing = Keychain.FindInternetUserNameAndPassword (uri, kind);
			if (existing != null && existing.Item1 != null && existing.Item2 != null)
				return new NetworkCredential (existing.Item1, existing.Item2);

			return null;
		}

		static ICredentials GetExistingCredentials (Uri uri, CredentialType credentialType)
		{
			if (credentialType == CredentialType.ProxyCredentials) {
				var proxyCreds = GetSystemProxyCredentials (uri);
				if (proxyCreds != null)
					return proxyCreds;
			}

			var rootUri = new Uri (uri.GetComponents (UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
			var existing =
				Keychain.FindInternetUserNameAndPassword (uri) ??
				Keychain.FindInternetUserNameAndPassword (rootUri);

			return existing != null ? new NetworkCredential (existing.Item1, existing.Item2) : null;
		}

		static ICredentials GetCredentialsFromUser (Uri uri, IWebProxy proxy, CredentialType credentialType)
		{
			NetworkCredential result = null;

			Runtime.RunInMainThread (() => {

				using (var ns = new NSAutoreleasePool ()) {
					var message = credentialType == CredentialType.ProxyCredentials
						? GettextCatalog.GetString (
							"{0} needs credentials to access the proxy server {1}.",
							BrandingService.ApplicationName,
							uri.Host
						)
						: GettextCatalog.GetString (
							"{0} needs credentials to access {1}.",
							BrandingService.ApplicationName,
							uri.Host
						);

					var alert = new NSAlert {
						MessageText = GettextCatalog.GetString ("Credentials Required"),
						InformativeText = message
					};

					var okButton = alert.AddButton (GettextCatalog.GetString ("OK"));
					var cancelButton = alert.AddButton (GettextCatalog.GetString ("Cancel"));

					alert.Icon = NSApplication.SharedApplication.ApplicationIconImage;

					var view = new NSView (new CGRect (0, 0, 313, 91));

					var usernameLabel = new NSTextField (new CGRect (17, 55, 71, 17)) {
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

					var usernameInput = new NSTextField (new CGRect (93, 52, 200, 22));
					view.AddSubview (usernameInput);

					var passwordLabel = new NSTextField (new CGRect (22, 23, 66, 17)) {
						StringValue = "Password:",
						Alignment = NSTextAlignment.Right,
						Editable = false,
						Bordered = false,
						DrawsBackground = false,
						Bezeled = false,
						Selectable = false,
					};
					view.AddSubview (passwordLabel);

					var passwordInput = new NSSecureTextField (new CGRect (93, 20, 200, 22));
					view.AddSubview (passwordInput);

					alert.AccessoryView = view;
					alert.Window.WeakDelegate = new PasswordAlertWindowDelegate (usernameInput, passwordInput, cancelButton, okButton);
					alert.Window.InitialFirstResponder = usernameInput;
					if (alert.RunModal () != NSAlertFirstButtonReturn)
						return;

					var username = usernameInput.StringValue;
					var password = passwordInput.StringValue;
					result = new NetworkCredential (username, password);
				}
			}).Wait ();

			// store the obtained credentials in the keychain
			// but don't store for the root url since it may have other credentials
			if (result != null)
				Keychain.AddInternetPassword (uri, result.UserName, result.Password);

			return result;
		}

		class PasswordAlertWindowDelegate : NSWindowDelegate
		{
			readonly WeakReference<NSTextField> weakUsernameInput;
			readonly WeakReference<NSTextField> weakPasswordInput;
			readonly WeakReference<NSButton> weakCancelButton;
			readonly WeakReference<NSButton> weakOkButton;

			public PasswordAlertWindowDelegate (NSTextField usernameInput, NSTextField passwordInput, NSButton cancelButton, NSButton okButton)
			{
				weakUsernameInput = new WeakReference<NSTextField> (usernameInput);
				weakPasswordInput = new WeakReference<NSTextField> (passwordInput);
				weakCancelButton = new WeakReference<NSButton> (cancelButton);
				weakOkButton = new WeakReference<NSButton> (okButton);
			}

			public override void DidBecomeKey (NSNotification notification)
			{
				if (!weakUsernameInput.TryGetTarget (out var usernameInput) ||
					!weakPasswordInput.TryGetTarget (out var passwordInput) ||
					!weakCancelButton.TryGetTarget (out var cancelButton) ||
					!weakOkButton.TryGetTarget (out var okButton))
					return;
				// The NSAlert defines the keyviewloop after it is displayed so the tab order is defined
				// here otherwise once the focus is on the OK and Cancel buttons it is not possible to tab
				// to the username and password NSTextFields.
				usernameInput.NextKeyView = passwordInput;
				passwordInput.NextKeyView = cancelButton;
				okButton.NextKeyView = usernameInput;
			}
		}
	}
}

