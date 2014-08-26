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

using MonoDevelop.Core;
using MonoDevelop.Ide;
using LibGit2Sharp;

namespace MonoDevelop.VersionControl.Git
{
	static class GitCredentials
	{
		public static Credentials TryGet (string url, string userFromUrl, SupportedCredentialTypes types)
		{
			bool result = false;
			var uri = new Uri (url);
			// We always need to run the TryGet* methods as we need the passphraseItem/passwordItem populated even
			// if the password store contains an invalid password/no password
			if ((types & SupportedCredentialTypes.UsernamePassword) != 0) {
				string username = string.Empty;
				string password = string.Empty;
				if (TryGetUsernamePassword (uri, out username, out password))
					return new UsernamePasswordCredentials {
						Username = username,
						Password = password
					};
			} /* no ssh support yet TryGetPassphrase (uri, out passphraseItem)*/

			var cred = new UsernamePasswordCredentials ();
			DispatchService.GuiSyncDispatch (delegate {
				var dlg = new CredentialsDialog (uri, types, cred);
				try {
					result = MessageService.ShowCustomDialog (dlg) == (int)Gtk.ResponseType.Ok;
				} finally {
					dlg.Destroy ();
				}
			});

			if (result) {
				if (!string.IsNullOrEmpty (cred.Password)) {
					PasswordService.AddWebUserNameAndPassword (uri, cred.Username, cred.Password);
				}/* else if (passphraseItem != null) {
					PasswordService.AddWebPassword (new Uri (uri), passphraseItem);
				}*/
			}

			return cred;
		}
		
		static bool TryGetPassphrase (Uri uri, out string passphrase)
		{
			var passphraseValue = PasswordService.GetWebPassword (uri);
			if (passphraseValue != null) {
				passphrase = passphraseValue;
				return true;
			}

			passphrase = null;
			return false;
		}
		
		static bool TryGetUsernamePassword (Uri uri, out string username, out string password)
		{
			var cred = PasswordService.GetWebUserNameAndPassword (uri);
			if (cred != null) {
				username = cred.Item1;
				password = cred.Item2;
				return true;
			}

			username = null;
			password = null;
			return false;
		}
	}
}

