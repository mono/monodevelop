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

using NGit;
using NGit.Transport;
using Sharpen;

namespace NGit
{
	/// <summary>Branch section of a Git configuration file.</summary>
	/// <remarks>Branch section of a Git configuration file.</remarks>
	public class BranchConfig
	{
		private readonly Config config;

		private readonly string branchName;

		/// <summary>
		/// Create a new branch config, which will read configuration from config
		/// about specified branch.
		/// </summary>
		/// <remarks>
		/// Create a new branch config, which will read configuration from config
		/// about specified branch.
		/// </remarks>
		/// <param name="config">the config to read from</param>
		/// <param name="branchName">the short branch name of the section to read</param>
		public BranchConfig(Config config, string branchName)
		{
			this.config = config;
			this.branchName = branchName;
		}

		/// <returns>
		/// the full remote-tracking branch name or <code>null</code> if it
		/// could not be determined
		/// </returns>
		public virtual string GetRemoteTrackingBranch()
		{
			string remote = GetRemote();
			string mergeRef = GetMergeBranch();
			if (remote == null || mergeRef == null)
			{
				return null;
			}
			RemoteConfig remoteConfig;
			try
			{
				remoteConfig = new RemoteConfig(config, remote);
			}
			catch (URISyntaxException)
			{
				return null;
			}
			foreach (RefSpec refSpec in remoteConfig.FetchRefSpecs)
			{
				if (refSpec.MatchSource(mergeRef))
				{
					RefSpec expanded = refSpec.ExpandFromSource(mergeRef);
					return expanded.GetDestination();
				}
			}
			return null;
		}

		private string GetRemote()
		{
			string remoteName = config.GetString(ConfigConstants.CONFIG_BRANCH_SECTION, branchName
				, ConfigConstants.CONFIG_KEY_REMOTE);
			if (remoteName == null)
			{
				return Constants.DEFAULT_REMOTE_NAME;
			}
			else
			{
				return remoteName;
			}
		}

		private string GetMergeBranch()
		{
			string mergeRef = config.GetString(ConfigConstants.CONFIG_BRANCH_SECTION, branchName
				, ConfigConstants.CONFIG_KEY_MERGE);
			return mergeRef;
		}
	}
}
