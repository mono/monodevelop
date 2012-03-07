/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System.Collections.Generic;
using NGit;
using NGit.Transport;
using Sharpen;

namespace NGit.Transport
{
	/// <summary>A remembered remote repository, including URLs and RefSpecs.</summary>
	/// <remarks>
	/// A remembered remote repository, including URLs and RefSpecs.
	/// <p>
	/// A remote configuration remembers one or more URLs for a frequently accessed
	/// remote repository as well as zero or more fetch and push specifications
	/// describing how refs should be transferred between this repository and the
	/// remote repository.
	/// </remarks>
	[System.Serializable]
	public class RemoteConfig
	{
		private const long serialVersionUID = 1L;

		private static readonly string SECTION = "remote";

		private static readonly string KEY_URL = "url";

		private static readonly string KEY_PUSHURL = "pushurl";

		private static readonly string KEY_FETCH = "fetch";

		private static readonly string KEY_PUSH = "push";

		private static readonly string KEY_UPLOADPACK = "uploadpack";

		private static readonly string KEY_RECEIVEPACK = "receivepack";

		private static readonly string KEY_TAGOPT = "tagopt";

		private static readonly string KEY_MIRROR = "mirror";

		private static readonly string KEY_TIMEOUT = "timeout";

		private static readonly string KEY_INSTEADOF = "insteadof";

		private static readonly string KEY_PUSHINSTEADOF = "pushinsteadof";

		private const bool DEFAULT_MIRROR = false;

		/// <summary>
		/// Default value for
		/// <see cref="UploadPack()">UploadPack()</see>
		/// if not specified.
		/// </summary>
		public static readonly string DEFAULT_UPLOAD_PACK = "git-upload-pack";

		/// <summary>
		/// Default value for
		/// <see cref="ReceivePack()">ReceivePack()</see>
		/// if not specified.
		/// </summary>
		public static readonly string DEFAULT_RECEIVE_PACK = "git-receive-pack";

		/// <summary>
		/// Parse all remote blocks in an existing configuration file, looking for
		/// remotes configuration.
		/// </summary>
		/// <remarks>
		/// Parse all remote blocks in an existing configuration file, looking for
		/// remotes configuration.
		/// </remarks>
		/// <param name="rc">
		/// the existing configuration to get the remote settings from.
		/// The configuration must already be loaded into memory.
		/// </param>
		/// <returns>
		/// all remotes configurations existing in provided repository
		/// configuration. Returned configurations are ordered
		/// lexicographically by names.
		/// </returns>
		/// <exception cref="Sharpen.URISyntaxException">one of the URIs within the remote's configuration is invalid.
		/// 	</exception>
		public static IList<NGit.Transport.RemoteConfig> GetAllRemoteConfigs(Config rc)
		{
			IList<string> names = new AList<string>(rc.GetSubsections(SECTION));
			names.Sort();
			IList<NGit.Transport.RemoteConfig> result = new AList<NGit.Transport.RemoteConfig
				>(names.Count);
			foreach (string name in names)
			{
				result.AddItem(new NGit.Transport.RemoteConfig(rc, name));
			}
			return result;
		}

		private string name;

		private string oldName;

		private IList<URIish> uris;

		private IList<URIish> pushURIs;

		private IList<RefSpec> fetch;

		private IList<RefSpec> push;

		private string uploadpack;

		private string receivepack;

		private NGit.Transport.TagOpt tagopt;

		private bool mirror;

		private int timeout;

		/// <summary>Parse a remote block from an existing configuration file.</summary>
		/// <remarks>
		/// Parse a remote block from an existing configuration file.
		/// <p>
		/// This constructor succeeds even if the requested remote is not defined
		/// within the supplied configuration file. If that occurs then there will be
		/// no URIs and no ref specifications known to the new instance.
		/// </remarks>
		/// <param name="rc">
		/// the existing configuration to get the remote settings from.
		/// The configuration must already be loaded into memory.
		/// </param>
		/// <param name="remoteName">subsection key indicating the name of this remote.</param>
		/// <exception cref="Sharpen.URISyntaxException">one of the URIs within the remote's configuration is invalid.
		/// 	</exception>
		public RemoteConfig(Config rc, string remoteName)
		{
			name = remoteName;
			oldName = remoteName;
			string[] vlst;
			string val;
			vlst = rc.GetStringList(SECTION, name, KEY_URL);
			IDictionary<string, string> insteadOf = GetReplacements(rc, KEY_INSTEADOF);
			uris = new AList<URIish>(vlst.Length);
			foreach (string s in vlst)
			{
				uris.AddItem(new URIish(ReplaceUri(s, insteadOf)));
			}
			IDictionary<string, string> pushInsteadOf = GetReplacements(rc, KEY_PUSHINSTEADOF
				);
			vlst = rc.GetStringList(SECTION, name, KEY_PUSHURL);
			pushURIs = new AList<URIish>(vlst.Length);
			foreach (string s_1 in vlst)
			{
				pushURIs.AddItem(new URIish(ReplaceUri(s_1, pushInsteadOf)));
			}
			vlst = rc.GetStringList(SECTION, name, KEY_FETCH);
			fetch = new AList<RefSpec>(vlst.Length);
			foreach (string s_2 in vlst)
			{
				fetch.AddItem(new RefSpec(s_2));
			}
			vlst = rc.GetStringList(SECTION, name, KEY_PUSH);
			push = new AList<RefSpec>(vlst.Length);
			foreach (string s_3 in vlst)
			{
				push.AddItem(new RefSpec(s_3));
			}
			val = rc.GetString(SECTION, name, KEY_UPLOADPACK);
			if (val == null)
			{
				val = DEFAULT_UPLOAD_PACK;
			}
			uploadpack = val;
			val = rc.GetString(SECTION, name, KEY_RECEIVEPACK);
			if (val == null)
			{
				val = DEFAULT_RECEIVE_PACK;
			}
			receivepack = val;
			val = rc.GetString(SECTION, name, KEY_TAGOPT);
			tagopt = NGit.Transport.TagOpt.FromOption(val);
			mirror = rc.GetBoolean(SECTION, name, KEY_MIRROR, DEFAULT_MIRROR);
			timeout = rc.GetInt(SECTION, name, KEY_TIMEOUT, 0);
		}

		/// <summary>Update this remote's definition within the configuration.</summary>
		/// <remarks>Update this remote's definition within the configuration.</remarks>
		/// <param name="rc">the configuration file to store ourselves into.</param>
		public virtual void Update(Config rc)
		{
			IList<string> vlst = new AList<string>();
			vlst.Clear();
			foreach (URIish u in URIs)
			{
				vlst.AddItem(u.ToPrivateString());
			}
			rc.SetStringList(SECTION, Name, KEY_URL, vlst);
			vlst.Clear();
			foreach (URIish u_1 in PushURIs)
			{
				vlst.AddItem(u_1.ToPrivateString());
			}
			rc.SetStringList(SECTION, Name, KEY_PUSHURL, vlst);
			vlst.Clear();
			foreach (RefSpec u_2 in FetchRefSpecs)
			{
				vlst.AddItem(u_2.ToString());
			}
			rc.SetStringList(SECTION, Name, KEY_FETCH, vlst);
			vlst.Clear();
			foreach (RefSpec u_3 in PushRefSpecs)
			{
				vlst.AddItem(u_3.ToString());
			}
			rc.SetStringList(SECTION, Name, KEY_PUSH, vlst);
			Set(rc, KEY_UPLOADPACK, UploadPack, DEFAULT_UPLOAD_PACK);
			Set(rc, KEY_RECEIVEPACK, ReceivePack, DEFAULT_RECEIVE_PACK);
			Set(rc, KEY_TAGOPT, TagOpt.Option(), NGit.Transport.TagOpt.AUTO_FOLLOW.Option());
			Set(rc, KEY_MIRROR, mirror, DEFAULT_MIRROR);
			Set(rc, KEY_TIMEOUT, timeout, 0);
			if (!oldName.Equals(name))
			{
				rc.UnsetSection(SECTION, oldName);
				oldName = name;
			}
		}

		private void Set(Config rc, string key, string currentValue, string defaultValue)
		{
			if (defaultValue.Equals(currentValue))
			{
				Unset(rc, key);
			}
			else
			{
				rc.SetString(SECTION, Name, key, currentValue);
			}
		}

		private void Set(Config rc, string key, bool currentValue, bool defaultValue)
		{
			if (defaultValue == currentValue)
			{
				Unset(rc, key);
			}
			else
			{
				rc.SetBoolean(SECTION, Name, key, currentValue);
			}
		}

		private void Set(Config rc, string key, int currentValue, int defaultValue)
		{
			if (defaultValue == currentValue)
			{
				Unset(rc, key);
			}
			else
			{
				rc.SetInt(SECTION, Name, key, currentValue);
			}
		}

		private void Unset(Config rc, string key)
		{
			rc.Unset(SECTION, Name, key);
		}

		private IDictionary<string, string> GetReplacements(Config config, string keyName
			)
		{
			IDictionary<string, string> replacements = new Dictionary<string, string>();
			foreach (string url in config.GetSubsections(KEY_URL))
			{
				foreach (string insteadOf in config.GetStringList(KEY_URL, url, keyName))
				{
					replacements.Put(insteadOf, url);
				}
			}
			return replacements;
		}

		private string ReplaceUri(string uri, IDictionary<string, string> replacements)
		{
			if (replacements.IsEmpty())
			{
				return uri;
			}
			KeyValuePair<string, string>? match = null;
			foreach (KeyValuePair<string, string> replacement in replacements.EntrySet())
			{
				// Ignore current entry if not longer than previous match
				if (match != null && match.Value.Key.Length > replacement.Key.Length)
				{
					continue;
				}
				if (!uri.StartsWith(replacement.Key))
				{
					continue;
				}
				match = replacement;
			}
			if (match != null)
			{
				return match.Value.Value + Sharpen.Runtime.Substring(uri, match.Value.Key.Length);
			}
			else
			{
				return uri;
			}
		}

		/// <summary>Get the local name this remote configuration is recognized as.</summary>
		/// <remarks>Get the local name this remote configuration is recognized as.</remarks>
		/// <returns>name assigned by the user to this configuration block.</returns>
		/// <summary>Set the local name this remote configuration is recognized as.</summary>
		/// <remarks>Set the local name this remote configuration is recognized as.</remarks>
		/// <value>the new name of this remote.</value>
		public virtual string Name
		{
			get
			{
				return name;
			}
			set
			{
				string newName = value;
				name = newName;
			}
		}

		/// <summary>Get all configured URIs under this remote.</summary>
		/// <remarks>Get all configured URIs under this remote.</remarks>
		/// <returns>the set of URIs known to this remote.</returns>
		public virtual IList<URIish> URIs
		{
			get
			{
				return Sharpen.Collections.UnmodifiableList(uris);
			}
		}

		/// <summary>Add a new URI to the end of the list of URIs.</summary>
		/// <remarks>Add a new URI to the end of the list of URIs.</remarks>
		/// <param name="toAdd">the new URI to add to this remote.</param>
		/// <returns>true if the URI was added; false if it already exists.</returns>
		public virtual bool AddURI(URIish toAdd)
		{
			if (uris.Contains(toAdd))
			{
				return false;
			}
			return uris.AddItem(toAdd);
		}

		/// <summary>Remove a URI from the list of URIs.</summary>
		/// <remarks>Remove a URI from the list of URIs.</remarks>
		/// <param name="toRemove">the URI to remove from this remote.</param>
		/// <returns>true if the URI was added; false if it already exists.</returns>
		public virtual bool RemoveURI(URIish toRemove)
		{
			return uris.Remove(toRemove);
		}

		/// <summary>Get all configured push-only URIs under this remote.</summary>
		/// <remarks>Get all configured push-only URIs under this remote.</remarks>
		/// <returns>the set of URIs known to this remote.</returns>
		public virtual IList<URIish> PushURIs
		{
			get
			{
				return Sharpen.Collections.UnmodifiableList(pushURIs);
			}
		}

		/// <summary>Add a new push-only URI to the end of the list of URIs.</summary>
		/// <remarks>Add a new push-only URI to the end of the list of URIs.</remarks>
		/// <param name="toAdd">the new URI to add to this remote.</param>
		/// <returns>true if the URI was added; false if it already exists.</returns>
		public virtual bool AddPushURI(URIish toAdd)
		{
			if (pushURIs.Contains(toAdd))
			{
				return false;
			}
			return pushURIs.AddItem(toAdd);
		}

		/// <summary>Remove a push-only URI from the list of URIs.</summary>
		/// <remarks>Remove a push-only URI from the list of URIs.</remarks>
		/// <param name="toRemove">the URI to remove from this remote.</param>
		/// <returns>true if the URI was added; false if it already exists.</returns>
		public virtual bool RemovePushURI(URIish toRemove)
		{
			return pushURIs.Remove(toRemove);
		}

		/// <summary>Remembered specifications for fetching from a repository.</summary>
		/// <remarks>Remembered specifications for fetching from a repository.</remarks>
		/// <returns>set of specs used by default when fetching.</returns>
		/// <summary>Override existing fetch specifications with new ones.</summary>
		/// <remarks>Override existing fetch specifications with new ones.</remarks>
		/// <value>
		/// list of fetch specifications to set. List is copied, it can be
		/// modified after this call.
		/// </value>
		public virtual IList<RefSpec> FetchRefSpecs
		{
			get
			{
				return Sharpen.Collections.UnmodifiableList(fetch);
			}
			set
			{
				IList<RefSpec> specs = value;
				fetch.Clear();
				Sharpen.Collections.AddAll(fetch, specs);
			}
		}

		/// <summary>Add a new fetch RefSpec to this remote.</summary>
		/// <remarks>Add a new fetch RefSpec to this remote.</remarks>
		/// <param name="s">the new specification to add.</param>
		/// <returns>true if the specification was added; false if it already exists.</returns>
		public virtual bool AddFetchRefSpec(RefSpec s)
		{
			if (fetch.Contains(s))
			{
				return false;
			}
			return fetch.AddItem(s);
		}

		/// <summary>Override existing push specifications with new ones.</summary>
		/// <remarks>Override existing push specifications with new ones.</remarks>
		/// <value>
		/// list of push specifications to set. List is copied, it can be
		/// modified after this call.
		/// </value>
		/// <summary>Remembered specifications for pushing to a repository.</summary>
		/// <remarks>Remembered specifications for pushing to a repository.</remarks>
		/// <returns>set of specs used by default when pushing.</returns>
		public virtual IList<RefSpec> PushRefSpecs
		{
			get
			{
				return Sharpen.Collections.UnmodifiableList(push);
			}
			set
			{
				IList<RefSpec> specs = value;
				push.Clear();
				Sharpen.Collections.AddAll(push, specs);
			}
		}

		/// <summary>Remove a fetch RefSpec from this remote.</summary>
		/// <remarks>Remove a fetch RefSpec from this remote.</remarks>
		/// <param name="s">the specification to remove.</param>
		/// <returns>true if the specification existed and was removed.</returns>
		public virtual bool RemoveFetchRefSpec(RefSpec s)
		{
			return fetch.Remove(s);
		}

		/// <summary>Add a new push RefSpec to this remote.</summary>
		/// <remarks>Add a new push RefSpec to this remote.</remarks>
		/// <param name="s">the new specification to add.</param>
		/// <returns>true if the specification was added; false if it already exists.</returns>
		public virtual bool AddPushRefSpec(RefSpec s)
		{
			if (push.Contains(s))
			{
				return false;
			}
			return push.AddItem(s);
		}

		/// <summary>Remove a push RefSpec from this remote.</summary>
		/// <remarks>Remove a push RefSpec from this remote.</remarks>
		/// <param name="s">the specification to remove.</param>
		/// <returns>true if the specification existed and was removed.</returns>
		public virtual bool RemovePushRefSpec(RefSpec s)
		{
			return push.Remove(s);
		}

		/// <summary>Override for the location of 'git-upload-pack' on the remote system.</summary>
		/// <remarks>
		/// Override for the location of 'git-upload-pack' on the remote system.
		/// <p>
		/// This value is only useful for an SSH style connection, where Git is
		/// asking the remote system to execute a program that provides the necessary
		/// network protocol.
		/// </remarks>
		/// <returns>
		/// location of 'git-upload-pack' on the remote system. If no
		/// location has been configured the default of 'git-upload-pack' is
		/// returned instead.
		/// </returns>
		public virtual string UploadPack
		{
			get
			{
				return uploadpack;
			}
		}

		/// <summary>Override for the location of 'git-receive-pack' on the remote system.</summary>
		/// <remarks>
		/// Override for the location of 'git-receive-pack' on the remote system.
		/// <p>
		/// This value is only useful for an SSH style connection, where Git is
		/// asking the remote system to execute a program that provides the necessary
		/// network protocol.
		/// </remarks>
		/// <returns>
		/// location of 'git-receive-pack' on the remote system. If no
		/// location has been configured the default of 'git-receive-pack' is
		/// returned instead.
		/// </returns>
		public virtual string ReceivePack
		{
			get
			{
				return receivepack;
			}
		}

		/// <summary>Get the description of how annotated tags should be treated during fetch.
		/// 	</summary>
		/// <remarks>Get the description of how annotated tags should be treated during fetch.
		/// 	</remarks>
		/// <returns>option indicating the behavior of annotated tags in fetch.</returns>
		/// <summary>Set the description of how annotated tags should be treated on fetch.</summary>
		/// <remarks>Set the description of how annotated tags should be treated on fetch.</remarks>
		/// <value>method to use when handling annotated tags.</value>
		public virtual NGit.Transport.TagOpt TagOpt
		{
			get
			{
				return tagopt;
			}
			set
			{
				NGit.Transport.TagOpt option = value;
				tagopt = option != null ? option : NGit.Transport.TagOpt.AUTO_FOLLOW;
			}
		}

		/// <returns>
		/// true if pushing to the remote automatically deletes remote refs
		/// which don't exist on the source side.
		/// </returns>
		/// <summary>Set the mirror flag to automatically delete remote refs.</summary>
		/// <remarks>Set the mirror flag to automatically delete remote refs.</remarks>
		/// <value>true to automatically delete remote refs during push.</value>
		public virtual bool IsMirror
		{
			get
			{
				return mirror;
			}
			set
			{
				bool m = value;
				mirror = m;
			}
		}

		/// <returns>timeout (in seconds) before aborting an IO operation.</returns>
		/// <summary>Set the timeout before willing to abort an IO call.</summary>
		/// <remarks>Set the timeout before willing to abort an IO call.</remarks>
		/// <value>
		/// number of seconds to wait (with no data transfer occurring)
		/// before aborting an IO read or write operation with this
		/// remote.  A timeout of 0 will block indefinitely.
		/// </value>
		public virtual int Timeout
		{
			get
			{
				return timeout;
			}
			set
			{
				int seconds = value;
				timeout = seconds;
			}
		}
	}
}
