// 
// GitCredentials.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using NGit.Transport;

namespace MonoDevelop.VersionControl.Git
{
	public class GitCredentials: CredentialsProvider
	{
		bool HasReset {
			get; set;
		}
		
		public override bool IsInteractive ()
		{
			return true;
		}
		
		public override bool Supports (params CredentialItem[] items)
		{
			return true;
		}

		public override bool Get (URIish uri, params CredentialItem[] items)
		{
			bool result = false;
			CredentialItem.Password passwordItem = null;
			CredentialItem.StringType passphraseItem = null;
			
			// We always need to run the TryGet* methods as we need the passphraseItem/passwordItem populated even
			// if the password store contains an invalid password/no password
			if (TryGetUsernamePassword (uri, items, out passwordItem) || TryGetPassphrase (uri, items, out passphraseItem)) {
				// If the password store has a password and we already tried using it, it could be incorrect.
				// If this happens, do not return true and ask the user for a new password.
				if (!HasReset) {
					return true;
				}
			}

			DispatchService.GuiSyncDispatch (delegate {
				CredentialsDialog dlg = new CredentialsDialog (uri, items);
				try {
					result = MessageService.ShowCustomDialog (dlg) == (int)Gtk.ResponseType.Ok;
				} finally {
					dlg.Destroy ();
				}
			});
				
			HasReset = false;
			if (result) {
				if (passwordItem != null) {
					PasswordService.AddWebPassword (new Uri (uri.ToString ()), new string (passwordItem.GetValue ()));
				} else if (passphraseItem != null) {
					PasswordService.AddWebPassword (new Uri (uri.ToString ()), passphraseItem.GetValue ());
				}
			}
			return result;
		}
		
		public override void Reset (URIish uri)
		{
			HasReset = true;
		}
		
		bool TryGetPassphrase (URIish uri, CredentialItem[] items, out CredentialItem.StringType passphraseItem)
		{
			var actualUrl = new Uri (uri.ToString ());
			var passphrase = (CredentialItem.StringType) items.FirstOrDefault (i => i is CredentialItem.StringType);
			
			if (items.Length == 1 && passphrase != null) {
				passphraseItem = passphrase;

				var passphraseValue = PasswordService.GetWebPassword (actualUrl);
				if (passphraseValue != null) {
					passphrase.SetValue (passphraseValue);
					return true;
				}
			} else {
				passphraseItem = null;
			}
			
			return false;
		}
		
		bool TryGetUsernamePassword (URIish uri, CredentialItem[] items, out CredentialItem.Password passwordItem)
		{
			var actualUrl = new Uri (uri.ToString ());
			var username = (CredentialItem.Username) items.FirstOrDefault (i => i is CredentialItem.Username);
			var password = (CredentialItem.Password) items.FirstOrDefault (i => i is CredentialItem.Password);

			if (items.Length == 2 && username != null && password != null) {
				passwordItem = password;

				var passwordValue = PasswordService.GetWebPassword (actualUrl);
				if (passwordValue != null) {
					username.SetValue (actualUrl.UserInfo);
					password.SetValueNoCopy (passwordValue.ToArray ());
					return true;
				}
			} else {
				passwordItem = null;
			}

			return false;
		}
	}
}

