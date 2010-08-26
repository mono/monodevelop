/*
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;

namespace GitSharp.Core.Transport
{
	/// <summary>
	/// A remembered remote repository, including URLs and RefSpecs.
	/// <para />
	/// A remote configuration remembers one or more URLs for a frequently accessed
	/// remote repository as well as zero or more fetch and push specifications
	/// describing how refs should be transferred between this repository and the
	/// remote repository.
	/// </summary>
	public class RemoteConfig
	{
		private const string Section = "remote";
		private const string KeyUrl = "url";
		private const string KeyPushurl = "pushurl";
		private const string KeyFetch = "fetch";
		private const string KeyPush = "push";
		private const string KeyUploadpack = "uploadpack";
		private const string KeyReceivepack = "receivepack";
		private const string KeyTagopt = "tagopt";
		private const string KeyMirror = "mirror";
		private const string KeyTimeout = "timeout";
		private const bool DefaultMirror = false;

		/// <summary>
		/// Default value for <see cref="UploadPack"/> if not specified.
		/// </summary>
		public const string DEFAULT_UPLOAD_PACK = "git-upload-pack";

		/// <summary>
		/// Default value for <see cref="ReceivePack"/> if not specified.
		/// </summary>
		public const string DEFAULT_RECEIVE_PACK = "git-receive-pack";

		/// <summary>
		/// Parse all remote blocks in an existing configuration file, looking for
		/// remotes configuration.
		/// </summary>
		/// <param name="rc">
		/// The existing configuration to get the remote settings from.
		/// The configuration must already be loaded into memory.
		/// </param>
		/// <returns>
		/// All remotes configurations existing in provided repository
		/// configuration. Returned configurations are ordered
		/// lexicographically by names.
		/// </returns>
		public static List<RemoteConfig> GetAllRemoteConfigs(RepositoryConfig rc)
		{
			if (rc == null)
				throw new ArgumentNullException ("rc");
			var names = new List<string>(rc.getSubsections(Section));
			names.Sort();

			var result = new List<RemoteConfig>(names.Count);
			foreach (string name in names)
			{
				result.Add(new RemoteConfig(rc, name));
			}

			return result;
		}

        /// <summary>
        /// local name this remote configuration is recognized as
        /// </summary>
		public string Name { get; private set; }

        /// <summary>
        /// all configured URIs under this remote
        /// </summary>
		public List<URIish> URIs { get; private set; }

        /// <summary>
        /// all configured push-only URIs under this remote.
        /// </summary>
		public List<URIish> PushURIs { get; private set; }

        /// <summary>
        /// Remembered specifications for fetching from a repository.
        /// </summary>
		public List<RefSpec> Fetch { get; private set; }

        /// <summary>
        /// Remembered specifications for pushing to a repository.
        /// </summary>
		public List<RefSpec> Push { get; private set; }

        /// <summary>
        /// Override for the location of 'git-upload-pack' on the remote system.
        /// <para/>
        /// This value is only useful for an SSH style connection, where Git is
        /// asking the remote system to execute a program that provides the necessary
        /// network protocol.
        /// <para/>
        /// returns location of 'git-upload-pack' on the remote system. If no
        /// location has been configured the default of 'git-upload-pack' is
        /// returned instead.
        /// </summary>
		public string UploadPack { get; private set; }

        /// <summary>
        /// Override for the location of 'git-receive-pack' on the remote system.
        /// <para/>
        /// This value is only useful for an SSH style connection, where Git is
        /// asking the remote system to execute a program that provides the necessary
        /// network protocol.
        /// <para/>
        /// returns location of 'git-receive-pack' on the remote system. If no
        /// location has been configured the default of 'git-receive-pack' is
        /// returned instead.
        /// </summary>
		public string ReceivePack { get; private set; }

        /// <summary>
        /// Get the description of how annotated tags should be treated during fetch.
        /// <para/>
        /// returns option indicating the behavior of annotated tags in fetch.
        /// </summary>
		public TagOpt TagOpt { get; private set; }

        /// <summary>
        /// mirror flag to automatically delete remote refs.
        /// <para/>
        /// true if pushing to the remote automatically deletes remote refs
        /// </summary>
		public bool Mirror { get; set; }

        /// <summary>
        /// Parse a remote block from an existing configuration file.
        /// <para/>
        /// This constructor succeeds even if the requested remote is not defined
        /// within the supplied configuration file. If that occurs then there will be
        /// no URIs and no ref specifications known to the new instance.
        /// </summary>
        /// <param name="rc">
        /// the existing configuration to get the remote settings from.
        /// The configuration must already be loaded into memory.
        /// </param>
        /// <param name="remoteName">subsection key indicating the name of this remote.</param>
		public RemoteConfig(Config rc, string remoteName)
		{
			if (rc == null)
				throw new ArgumentNullException ("rc");
			Name = remoteName;

			string[] vlst = rc.getStringList(Section, Name, KeyUrl);
			URIs = new List<URIish>(vlst.Length);
			foreach (string s in vlst)
			{
				URIs.Add(new URIish(s));
			}

			vlst = rc.getStringList(Section, Name, KeyPushurl);
			PushURIs = new List<URIish>(vlst.Length);
			foreach (string s in vlst)
			{
				PushURIs.Add(new URIish(s));
			}

			vlst = rc.getStringList(Section, Name, KeyFetch);
			Fetch = new List<RefSpec>(vlst.Length);
			foreach (string s in vlst)
			{
				Fetch.Add(new RefSpec(s));
			}

			vlst = rc.getStringList(Section, Name, KeyPush);
			Push = new List<RefSpec>(vlst.Length);
			foreach (string s in vlst)
			{
				Push.Add(new RefSpec(s));
			}

			string val = rc.getString(Section, Name, KeyUploadpack) ?? DEFAULT_UPLOAD_PACK;
			UploadPack = val;

			val = rc.getString(Section, Name, KeyReceivepack) ?? DEFAULT_RECEIVE_PACK;
			ReceivePack = val;

			val = rc.getString(Section, Name, KeyTagopt);
			TagOpt = TagOpt.fromOption(val);
			Mirror = rc.getBoolean(Section, Name, KeyMirror, DefaultMirror);

			Timeout = rc.getInt(Section, Name, KeyTimeout, 0);
		}

        /// <summary>
        /// Update this remote's definition within the configuration.
        /// </summary>
        /// <param name="rc">the configuration file to store ourselves into.</param>
		public void Update(Config rc)
		{
			if (rc == null)
				throw new ArgumentNullException ("rc");
			var vlst = new List<string>();

			vlst.Clear();
			foreach (URIish u in URIs)
			{
				vlst.Add(u.ToPrivateString());
			}
			rc.setStringList(Section, Name, KeyUrl, vlst);

			vlst.Clear();
			foreach (URIish u in PushURIs)
				vlst.Add(u.ToPrivateString());
			rc.setStringList(Section, Name, KeyPushurl, vlst);

			vlst.Clear();
			foreach (RefSpec u in Fetch)
			{
				vlst.Add(u.ToString());
			}
			rc.setStringList(Section, Name, KeyFetch, vlst);

			vlst.Clear();
			foreach (RefSpec u in Push)
			{
				vlst.Add(u.ToString());
			}
			rc.setStringList(Section, Name, KeyPush, vlst);

			Set(rc, KeyUploadpack, UploadPack, DEFAULT_UPLOAD_PACK);
			Set(rc, KeyReceivepack, ReceivePack, DEFAULT_RECEIVE_PACK);
			Set(rc, KeyTagopt, TagOpt.Option, TagOpt.AUTO_FOLLOW.Option);
			Set(rc, KeyMirror, Mirror, DefaultMirror);
			Set(rc, KeyTimeout, Timeout, 0);
		}

		private void Set(Config rc, string key, string currentValue, IEquatable<string> defaultValue)
		{
			if (defaultValue.Equals(currentValue))
			{
				Unset(rc, key);
			}
			else
			{
				rc.setString(Section, Name, key, currentValue);
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
                rc.setBoolean(Section, Name, key, currentValue);
            }
        }
        
        private void Set(Config rc, string key, int currentValue, IEquatable<int> defaultValue)
		{
			if (defaultValue.Equals(currentValue))
			{
				Unset(rc, key);
			}
			else
			{
				rc.setInt(Section, Name, key, currentValue);
			}
		}

		private void Unset(Config rc, string key)
		{
			rc.unset(Section, Name, key);
		}

        /// <summary>
        /// Add a new URI to the end of the list of URIs.
        /// </summary>
        /// <param name="toAdd">the new URI to add to this remote.</param>
        /// <returns>true if the URI was added; false if it already exists.</returns>
		public bool AddURI(URIish toAdd)
		{
			if (URIs.Contains(toAdd)) return false;

			URIs.Add(toAdd);
			return true;
		}

        /// <summary>
        /// Remove a URI from the list of URIs.
        /// </summary>
        /// <param name="toRemove">the URI to remove from this remote.</param>
        /// <returns>true if the URI was added; false if it already exists.</returns>
		public bool RemoveURI(URIish toRemove)
		{
			return URIs.Remove(toRemove);
		}

        /// <summary>
        /// Add a new push-only URI to the end of the list of URIs.
        /// </summary>
        /// <param name="toAdd">the new URI to add to this remote.</param>
        /// <returns>true if the URI was added; false if it already exists.</returns>
		public bool AddPushURI(URIish toAdd)
		{
			if (PushURIs.Contains(toAdd)) return false;

			PushURIs.Add(toAdd);
			return true;
		}

        /// <summary>
        /// Remove a push-only URI from the list of URIs.
        /// </summary>
        /// <param name="toRemove">the URI to remove from this remote.</param>
        /// <returns>true if the URI was added; false if it already exists.</returns>
        public bool RemovePushURI(URIish toRemove)
        {
            return PushURIs.Remove(toRemove);
        }

        /// <summary>
        /// Add a new fetch RefSpec to this remote.
        /// </summary>
        /// <param name="s">the new specification to add.</param>
        /// <returns>true if the specification was added; false if it already exists.</returns>
        public bool AddFetchRefSpec(RefSpec s)
        {
            if (Fetch.Contains(s))
            {
                return false;
            }

            Fetch.Add(s);

            return true;
        }

        /// <summary>
        /// Override existing fetch specifications with new ones.
        /// </summary>
        /// <param name="specs">
        /// list of fetch specifications to set. List is copied, it can be
        /// modified after this call.
        /// </param>
		public void SetFetchRefSpecs(List<RefSpec> specs)
		{
			Fetch.Clear();
			Fetch.AddRange(specs);
		}

        /// <summary>
        /// Override existing push specifications with new ones.
        /// </summary>
        /// <param name="specs">
        /// list of push specifications to set. List is copied, it can be
        /// modified after this call.
        /// </param>
		public void SetPushRefSpecs(List<RefSpec> specs)
		{
			Push.Clear();
			Push.AddRange(specs);
		}

		/// <summary>
        /// Remove a fetch RefSpec from this remote.
		/// </summary>
        /// <param name="s">the specification to remove.</param>
        /// <returns>true if the specification existed and was removed.</returns>
        public bool RemoveFetchRefSpec(RefSpec s)
		{
			return Fetch.Remove(s);
		}

        /// <summary>
        /// Add a new push RefSpec to this remote.
        /// </summary>
        /// <param name="s">the new specification to add.</param>
        /// <returns>true if the specification was added; false if it already exists.</returns>
		public bool AddPushRefSpec(RefSpec s)
		{
			if (Push.Contains(s)) return false;

			Push.Add(s);
			return true;
		}

        /// <summary>
        /// Remove a push RefSpec from this remote.
        /// </summary>
        /// <param name="s">the specification to remove.</param>
        /// <returns>true if the specification existed and was removed.</returns>
		public bool RemovePushRefSpec(RefSpec s)
		{
			return Push.Remove(s);
		}

        /// <summary>
        /// Set the description of how annotated tags should be treated on fetch.
        /// </summary>
        /// <param name="option">method to use when handling annotated tags.</param>
		public void SetTagOpt(TagOpt option)
		{
			TagOpt = option ?? TagOpt.AUTO_FOLLOW;
		}

        /// <summary>
        /// timeout before willing to abort an IO call.
        /// <para/>
        /// number of seconds to wait (with no data transfer occurring)
        /// before aborting an IO read or write operation with this
        /// remote.  A timeout of 0 will block indefinitely.
        /// </summary>
		public int Timeout { get; set; }
	}
}