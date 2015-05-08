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
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Components;

namespace MonoDevelop.VersionControl.Git
{
	static class GitCredentials
	{
		// Gather keys on initialize.
		static readonly List<string> Keys = new List<string> ();
		static readonly Dictionary<string, int> KeyForUrl = new Dictionary<string, int> ();
		static readonly Dictionary<string, bool> AgentForUrl = new Dictionary<string, bool> ();

		static string urlUsed;
		static bool agentUsed;
		static int keyUsed = -1;
		static bool nativePasswordUsed;

		static GitCredentials ()
		{
			string keyStorage = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".ssh");
			if (!Directory.Exists (keyStorage))
				return;

			foreach (var privateKey in Directory.EnumerateFiles (keyStorage)) {
				string publicKey = privateKey + ".pub";
				if (File.Exists (publicKey) && !KeyHasPassphrase (privateKey))
					Keys.Add (privateKey);
			}
		}

		public static Credentials TryGet (string url, string userFromUrl, SupportedCredentialTypes types)
		{
			bool result = true;
			Uri uri = null;

			urlUsed = url;

			// We always need to run the TryGet* methods as we need the passphraseItem/passwordItem populated even
			// if the password store contains an invalid password/no password
			if ((types & SupportedCredentialTypes.UsernamePassword) != 0) {
				uri = new Uri (url);
				string username;
				string password;
				if (!nativePasswordUsed && TryGetUsernamePassword (uri, out username, out password)) {
					nativePasswordUsed = true;
					return new UsernamePasswordCredentials {
						Username = username,
						Password = password
					};
				}
			}

			Credentials cred;
			if ((types & SupportedCredentialTypes.UsernamePassword) != 0)
				cred = new UsernamePasswordCredentials ();
			else {
				// Try ssh-agent on Linux.
				if (!Platform.IsWindows && !agentUsed) {
					bool agentUsable;
					if (!AgentForUrl.TryGetValue (url, out agentUsable))
						AgentForUrl [url] = agentUsable = true;

					if (agentUsable) {
						agentUsed = true;
						return new SshAgentCredentials {
							Username = userFromUrl,
						};
					}
				}

				int key;
				if (!KeyForUrl.TryGetValue (url, out key)) {
					if (keyUsed + 1 < Keys.Count)
						keyUsed++;
					else {
						SelectFileDialog dlg = null;
						bool success = false;

						DispatchService.GuiSyncDispatch (() => {
							dlg = new SelectFileDialog (GettextCatalog.GetString ("Select a private SSH key to use."));
							dlg.ShowHidden = true;
							dlg.CurrentFolder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
							success = dlg.Run ();
						});
						if (!success || !File.Exists (dlg.SelectedFile + ".pub"))
							throw new VersionControlException (GettextCatalog.GetString ("Invalid credentials were supplied. Aborting operation."));

						cred = new SshUserKeyCredentials {
							Username = userFromUrl,
							Passphrase = "",
							PrivateKey = dlg.SelectedFile,
							PublicKey = dlg.SelectedFile + ".pub",
						};

						if (KeyHasPassphrase (dlg.SelectedFile)) {
							DispatchService.GuiSyncDispatch (delegate {
								result = MessageService.ShowCustomDialog (new CredentialsDialog (url, types, cred)) == (int)Gtk.ResponseType.Ok;
							});
						}

						if (result)
							return cred;
						throw new VersionControlException (GettextCatalog.GetString ("Invalid credentials were supplied. Aborting operation."));
					}
				} else
					keyUsed = key;

				cred = new SshUserKeyCredentials {
					Username = userFromUrl,
					Passphrase = "",
					PrivateKey = Keys [keyUsed],
					PublicKey = Keys [keyUsed] + ".pub",
				};
				return cred;
			}

			DispatchService.GuiSyncDispatch (delegate {
				result = MessageService.ShowCustomDialog (new CredentialsDialog (url, types, cred)) == (int)Gtk.ResponseType.Ok;
			});

			if (result) {
				if ((types & SupportedCredentialTypes.UsernamePassword) != 0) {
					var upcred = (UsernamePasswordCredentials)cred;
					if (!string.IsNullOrEmpty (upcred.Password)) {
						PasswordService.AddWebUserNameAndPassword (uri, upcred.Username, upcred.Password);
					}
				}
			}

			return cred;
		}

		static bool KeyHasPassphrase (string key)
		{
			return File.ReadAllText (key).Contains ("Proc-Type: 4,ENCRYPTED");
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

		internal static void StoreCredentials ()
		{
			nativePasswordUsed = false;

			if (!string.IsNullOrEmpty (urlUsed))
				if (keyUsed != -1)
					KeyForUrl [urlUsed] = keyUsed;

			Cleanup ();
		}

		internal static void InvalidateCredentials ()
		{
			if (!string.IsNullOrEmpty (urlUsed))
				if (AgentForUrl.ContainsKey (urlUsed))
					AgentForUrl [urlUsed] &= !agentUsed;

			Cleanup ();
		}

		static void Cleanup ()
		{
			urlUsed = null;
			agentUsed = false;
			keyUsed = -1;
		}
	}
}
