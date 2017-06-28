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
	public enum GitCredentialsType
	{
		Normal,
		Tfs,
	}

	public class GitCredentialsState
	{
		public string UrlUsed { get; set; }
		public bool AgentUsed { get; set; }
		public int KeyUsed { get; set; }
		public bool NativePasswordUsed { get; set; }
		public Dictionary<string, int> KeyForUrl = new Dictionary<string, int> ();
		public Dictionary<string, bool> AgentForUrl = new Dictionary<string, bool> ();

		public GitCredentialsState ()
		{
			KeyUsed = -1;
		}
	}

	static class GitCredentials
	{
		// Gather keys on initialize.
		static readonly List<string> Keys = new List<string> ();

		static Dictionary<GitCredentialsType, GitCredentialsState> credState = new Dictionary<GitCredentialsType, GitCredentialsState> ();

		static GitCredentials ()
		{
			string keyStorage = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".ssh");
			if (!Directory.Exists (keyStorage)) {
				keyStorage = Path.Combine (Environment.ExpandEnvironmentVariables ("%HOME%"), ".ssh");
				if (!Directory.Exists (keyStorage))
					return;
			}

			foreach (var privateKey in Directory.EnumerateFiles (keyStorage)) {
				string publicKey = privateKey + ".pub";
				if (File.Exists (publicKey) && !KeyHasPassphrase (privateKey))
					Keys.Add (privateKey);
			}
		}

		public static Credentials TryGet (string url, string userFromUrl, SupportedCredentialTypes types, GitCredentialsType type)
		{
			bool result = true;
			Uri uri = null;

			GitCredentialsState state;
			if (!credState.TryGetValue (type, out state))
				credState [type] = state = new GitCredentialsState ();
			state.UrlUsed = url;

			// We always need to run the TryGet* methods as we need the passphraseItem/passwordItem populated even
			// if the password store contains an invalid password/no password
			if ((types & SupportedCredentialTypes.UsernamePassword) != 0) {
				if (Uri.TryCreate (url, UriKind.RelativeOrAbsolute, out uri)) {
					string username;
					string password;
					if (!state.NativePasswordUsed && TryGetUsernamePassword (uri, out username, out password)) {
						state.NativePasswordUsed = true;
						return new UsernamePasswordCredentials {
							Username = username,
							Password = password
						};
					}
				}
			}

			Credentials cred;
			if ((types & SupportedCredentialTypes.UsernamePassword) != 0)
				cred = new UsernamePasswordCredentials ();
			else {
				// Try ssh-agent on Linux.
				if (!Platform.IsWindows && !state.AgentUsed) {
					bool agentUsable;
					if (!state.AgentForUrl.TryGetValue (url, out agentUsable))
						state.AgentForUrl [url] = agentUsable = true;

					if (agentUsable) {
						state.AgentUsed = true;
						return new SshAgentCredentials {
							Username = userFromUrl,
						};
					}
				}

				int key;
				if (!state.KeyForUrl.TryGetValue (url, out key)) {
					if (state.KeyUsed + 1 < Keys.Count)
						state.KeyUsed++;
					else {
						SelectFileDialog dlg = null;
						bool success = Runtime.RunInMainThread (() => {
							dlg = new SelectFileDialog (GettextCatalog.GetString ("Select a private SSH key to use."));
							dlg.ShowHidden = true;
							dlg.CurrentFolder = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
							return dlg.Run ();
						}).Result;
						if (!success || !File.Exists (dlg.SelectedFile + ".pub"))
							throw new VersionControlException (GettextCatalog.GetString ("Invalid credentials were supplied. Aborting operation."));

						cred = new SshUserKeyCredentials {
							Username = userFromUrl,
							Passphrase = "",
							PrivateKey = dlg.SelectedFile,
							PublicKey = dlg.SelectedFile + ".pub",
						};

						if (KeyHasPassphrase (dlg.SelectedFile)) {
							result = Runtime.RunInMainThread (delegate {
								using (var credDlg = new CredentialsDialog (url, types, cred))
									return MessageService.ShowCustomDialog (credDlg) == (int)Gtk.ResponseType.Ok;
							}).Result;
						}

						if (result)
							return cred;
						throw new VersionControlException (GettextCatalog.GetString ("Invalid credentials were supplied. Aborting operation."));
					}
				} else
					state.KeyUsed = key;

				cred = new SshUserKeyCredentials {
					Username = userFromUrl,
					Passphrase = "",
					PrivateKey = Keys [state.KeyUsed],
					PublicKey = Keys [state.KeyUsed] + ".pub",
				};
				return cred;
			}

			result = Runtime.RunInMainThread (delegate {
				using (var credDlg = new CredentialsDialog (url, types, cred))
					return MessageService.ShowCustomDialog (credDlg) == (int)Gtk.ResponseType.Ok;
			}).Result;

			if (result) {
				if ((types & SupportedCredentialTypes.UsernamePassword) != 0) {
					var upcred = (UsernamePasswordCredentials)cred;
					if (!string.IsNullOrEmpty (upcred.Password) && uri != null) {
						PasswordService.AddWebUserNameAndPassword (uri, upcred.Username, upcred.Password);
					}
				}
				return cred;
			}

			throw new VersionControlException (GettextCatalog.GetString ("Operation cancelled by the user"));
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

		internal static void StoreCredentials (GitCredentialsType type)
		{
			GitCredentialsState state;
			if (!credState.TryGetValue (type, out state))
				return;

			var url = state.UrlUsed;
			var key = state.KeyUsed;

			state.NativePasswordUsed = false;

			if (!string.IsNullOrEmpty (url) && key != -1)
				state.KeyForUrl [url] = key;

			Cleanup (state);
		}

		internal static void InvalidateCredentials (GitCredentialsType type)
		{
			GitCredentialsState state;
			if (!credState.TryGetValue (type, out state))
				return;

			if (!string.IsNullOrEmpty (state.UrlUsed) && state.AgentForUrl.ContainsKey (state.UrlUsed))
				state.AgentForUrl [state.UrlUsed] &= !state.AgentUsed;

			Cleanup (state);
		}

		static void Cleanup (GitCredentialsState state)
		{
			state.UrlUsed = null;
			state.AgentUsed = false;
			state.KeyUsed = -1;
		}
	}
}
