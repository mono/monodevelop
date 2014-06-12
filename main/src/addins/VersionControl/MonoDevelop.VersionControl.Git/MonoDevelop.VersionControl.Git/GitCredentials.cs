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
using LibGit2Sharp.Core;

namespace MonoDevelop.VersionControl.Git
{
	static class GitCredentials
	{
		public static bool TryGetHandles (Uri uri, out string username, out string password)
		{
			bool result = false;
			// We always need to run the TryGet* methods as we need the passphraseItem/passwordItem populated even
			// if the password store contains an invalid password/no password
			if (TryGetUsernamePassword (uri, out username, out password)/* || TryGetPassphrase (uri, items, out passphraseItem)*/)
				return true;

			string tempuser = String.Empty;
			string temppass = string.Empty;

			DispatchService.GuiSyncDispatch (delegate {
				var dlg = new CredentialsDialog (uri, GitCredentialType.UserPassPlaintext);
				try {
					result = MessageService.ShowCustomDialog (dlg) == (int)Gtk.ResponseType.Ok;
					tempuser = dlg.Username;
					temppass = dlg.Password;
				} finally {
					dlg.Destroy ();
				}
			});

			username = tempuser;
			password = temppass;

			if (result) {
				if (password != null) {
					PasswordService.AddWebUserNameAndPassword (uri, username, password);
				}/* else if (passphraseItem != null) {
					PasswordService.AddWebPassword (new Uri (uri), passphraseItem);
				}*/
			}
			return result;
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

