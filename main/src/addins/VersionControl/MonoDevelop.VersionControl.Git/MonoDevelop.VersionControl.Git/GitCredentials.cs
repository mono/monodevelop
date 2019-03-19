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
using System.Linq;
using Mono.Addins;

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
		internal static readonly string UserCancelledExceptionMessage = GettextCatalog.GetString("Operation cancelled");

		// Gather keys on initialize.
		static readonly List<string> Keys = new List<string> ();
		static readonly List<string> PublicKeys = new List<string> ();

		static Dictionary<GitCredentialsType, GitCredentialsState> credState = new Dictionary<GitCredentialsType, GitCredentialsState> ();

		static GitCredentials ()
		{
			string keyStorage = Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), ".ssh");
			if (!Directory.Exists (keyStorage)) {
				keyStorage = Path.Combine (Environment.ExpandEnvironmentVariables ("%HOME%"), ".ssh");
				if (!Directory.Exists (keyStorage))
					return;
			}

			var defaultKey = FilePath.Null;

			foreach (FilePath privateKey in Directory.EnumerateFiles (keyStorage)) {
				if (privateKey.Extension == ".pub")
					continue;
				string publicKey = privateKey + ".pub";
				if (File.Exists (publicKey)) {
					if (privateKey.FileName == "id_rsa")
						defaultKey = privateKey;
					else if (!KeyHasPassphrase (privateKey)) {
						Keys.Add (privateKey);
						PublicKeys.Add (publicKey);
					}
				}
			}

			if (defaultKey.IsNotNull) {
				var publicKey = defaultKey + ".pub";
				// if the default key has no passphrase, make it the first key to try when authenticating,
				// or the last one otherwise, to make sure that we try the unprotected keys first, before prompting
				// for the passphrase.
				if (KeyHasPassphrase (defaultKey)) {
					Keys.Add (defaultKey);
					PublicKeys.Add (publicKey);
				} else {
					Keys.Insert (0, defaultKey);
					PublicKeys.Insert (0, publicKey);
				}

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
					if (!state.NativePasswordUsed && TryGetUsernamePassword (uri, out var username, out var password)) {
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

				int keyIndex;
				if (state.KeyForUrl.TryGetValue (url, out keyIndex))
					state.KeyUsed = keyIndex;
				else {
					if (state.KeyUsed + 1 < Keys.Count)
						state.KeyUsed++;
					else {
						var sshCred = new SshUserKeyCredentials {
							Username = userFromUrl,
							Passphrase = string.Empty
						};
						cred = sshCred;

						if (XwtCredentialsDialog.Run (url, types, cred).Result) {
							keyIndex = Keys.IndexOf (sshCred.PrivateKey);
							if (keyIndex < 0) {
								Keys.Add (sshCred.PrivateKey);
								PublicKeys.Add (sshCred.PublicKey);
								state.KeyUsed++;
							} else
								state.KeyUsed = keyIndex;
							return cred;
						}
						throw new UserCancelledException (UserCancelledExceptionMessage);
					}
				}

				var key = Keys [state.KeyUsed];
				cred = new SshUserKeyCredentials {
					Username = userFromUrl,
					Passphrase = string.Empty,
					PrivateKey = key,
					PublicKey = PublicKeys [state.KeyUsed]
				};

				if (KeyHasPassphrase (key)) {
					if (XwtCredentialsDialog.Run (url, types, cred).Result) {
						var sshCred = (SshUserKeyCredentials)cred;
						keyIndex = Keys.IndexOf (sshCred.PrivateKey);
						if (keyIndex < 0) {
							Keys.Add (sshCred.PrivateKey);
							PublicKeys.Add (sshCred.PublicKey);
							state.KeyUsed++;
						} else
							state.KeyUsed = keyIndex;
					} else
						throw new UserCancelledException (UserCancelledExceptionMessage);
				}

				return cred;
			}

			var gitCredentialsProviders = AddinManager.GetExtensionObjects<IGitCredentialsProvider> ();

			if (gitCredentialsProviders != null) {
				foreach (var gitCredentialsProvider in gitCredentialsProviders) {
					if (gitCredentialsProvider.SupportsUrl (url)) {
						var providerResult = GetCredentialsFromProvider (gitCredentialsProvider, url, types, cred);
						if (providerResult == GitCredentialsProviderResult.Cancelled)
							throw new UserCancelledException (UserCancelledExceptionMessage);
						if (result = providerResult == GitCredentialsProviderResult.Found)
							break;
					}
				}
			}

			if (!result) {
				result = GetCredentials (url, types, cred);
			}

			if (result) {
				if ((types & SupportedCredentialTypes.UsernamePassword) != 0) {
					var upcred = (UsernamePasswordCredentials)cred;
					if (!string.IsNullOrEmpty (upcred.Password) && uri != null) {
						PasswordService.AddWebUserNameAndPassword (uri, upcred.Username, upcred.Password);
					}
				}
				return cred;
			}

			throw new UserCancelledException (UserCancelledExceptionMessage);
		}

		static GitCredentialsProviderResult GetCredentialsFromProvider (IGitCredentialsProvider gitCredentialsProvider, string uri, SupportedCredentialTypes type, Credentials cred)
		{
			if (type != SupportedCredentialTypes.UsernamePassword)
				return GitCredentialsProviderResult.NotFound;

			var (result, credentials) = gitCredentialsProvider.TryGetCredentialsAsync (uri).Result;
		
			if (result == GitCredentialsProviderResult.Found) {
				((UsernamePasswordCredentials)cred).Username = credentials.Username;
				((UsernamePasswordCredentials)cred).Password = credentials.Password;
			}

			return result;
		}

		static bool GetCredentials (string uri, SupportedCredentialTypes type, Credentials cred)
		{
			return XwtCredentialsDialog.Run (uri, type, cred).Result;
		}

		internal static bool KeyHasPassphrase (string key)
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
			// if the Uri has a path, fallback to base Uri if available
			if (cred == null && !string.IsNullOrEmpty (uri.PathAndQuery) && Uri.TryCreate (uri.GetLeftPart (UriPartial.Authority), UriKind.Absolute, out var baseUri)) {
				cred = PasswordService.GetWebUserNameAndPassword (baseUri);
			}
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
