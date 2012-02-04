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
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Constants for use with the Configuration classes: section names,
	/// configuration keys
	/// </summary>
	public class ConfigConstants
	{
		/// <summary>The "core" section</summary>
		public static readonly string CONFIG_CORE_SECTION = "core";

		/// <summary>The "branch" section</summary>
		public static readonly string CONFIG_BRANCH_SECTION = "branch";

		/// <summary>The "remote" section</summary>
		public static readonly string CONFIG_REMOTE_SECTION = "remote";

		/// <summary>The "diff" section</summary>
		public static readonly string CONFIG_DIFF_SECTION = "diff";

		/// <summary>The "dfs" section</summary>
		public static readonly string CONFIG_DFS_SECTION = "dfs";

		/// <summary>The "user" section</summary>
		public static readonly string CONFIG_USER_SECTION = "user";

		/// <summary>The "gerrit" section</summary>
		public static readonly string CONFIG_GERRIT_SECTION = "gerrit";

		/// <summary>The "workflow" section</summary>
		public static readonly string CONFIG_WORKFLOW_SECTION = "workflow";

		/// <summary>The "submodule" section</summary>
		public static readonly string CONFIG_SUBMODULE_SECTION = "submodule";

		/// <summary>The "pack" section</summary>
		public static readonly string CONFIG_PACK_SECTION = "pack";

		/// <summary>The "algorithm" key</summary>
		public static readonly string CONFIG_KEY_ALGORITHM = "algorithm";

		/// <summary>The "autocrlf" key</summary>
		public static readonly string CONFIG_KEY_AUTOCRLF = "autocrlf";

		/// <summary>The "bare" key</summary>
		public static readonly string CONFIG_KEY_BARE = "bare";

		/// <summary>The "excludesfile" key</summary>
		public static readonly string CONFIG_KEY_EXCLUDESFILE = "excludesfile";

		/// <summary>The "filemode" key</summary>
		public static readonly string CONFIG_KEY_FILEMODE = "filemode";

		/// <summary>The "logallrefupdates" key</summary>
		public static readonly string CONFIG_KEY_LOGALLREFUPDATES = "logallrefupdates";

		/// <summary>The "repositoryformatversion" key</summary>
		public static readonly string CONFIG_KEY_REPO_FORMAT_VERSION = "repositoryformatversion";

		/// <summary>The "worktree" key</summary>
		public static readonly string CONFIG_KEY_WORKTREE = "worktree";

		/// <summary>The "blockLimit" key</summary>
		public static readonly string CONFIG_KEY_BLOCK_LIMIT = "blockLimit";

		/// <summary>The "blockSize" key</summary>
		public static readonly string CONFIG_KEY_BLOCK_SIZE = "blockSize";

		/// <summary>The "readAheadLimit" key</summary>
		public static readonly string CONFIG_KEY_READ_AHEAD_LIMIT = "readAheadLimit";

		/// <summary>The "readAheadThreads" key</summary>
		public static readonly string CONFIG_KEY_READ_AHEAD_THREADS = "readAheadThreads";

		/// <summary>The "deltaBaseCacheLimit" key</summary>
		public static readonly string CONFIG_KEY_DELTA_BASE_CACHE_LIMIT = "deltaBaseCacheLimit";

		/// <summary>The "streamFileThreshold" key</summary>
		public static readonly string CONFIG_KEY_STREAM_FILE_TRESHOLD = "streamFileThreshold";

		/// <summary>The "remote" key</summary>
		public static readonly string CONFIG_KEY_REMOTE = "remote";

		/// <summary>The "merge" key</summary>
		public static readonly string CONFIG_KEY_MERGE = "merge";

		/// <summary>The "rebase" key</summary>
		public static readonly string CONFIG_KEY_REBASE = "rebase";

		/// <summary>The "url" key</summary>
		public static readonly string CONFIG_KEY_URL = "url";

		/// <summary>The "autosetupmerge" key</summary>
		public static readonly string CONFIG_KEY_AUTOSETUPMERGE = "autosetupmerge";

		/// <summary>The "autosetuprebase" key</summary>
		public static readonly string CONFIG_KEY_AUTOSETUPREBASE = "autosetuprebase";

		/// <summary>The "name" key</summary>
		public static readonly string CONFIG_KEY_NAME = "name";

		/// <summary>The "email" key</summary>
		public static readonly string CONFIG_KEY_EMAIL = "email";

		/// <summary>
		/// The "false" key (used to configure
		/// <see cref="CONFIG_KEY_AUTOSETUPMERGE">CONFIG_KEY_AUTOSETUPMERGE</see>
		/// 
		/// </summary>
		public static readonly string CONFIG_KEY_FALSE = "false";

		/// <summary>
		/// The "true" key (used to configure
		/// <see cref="CONFIG_KEY_AUTOSETUPMERGE">CONFIG_KEY_AUTOSETUPMERGE</see>
		/// 
		/// </summary>
		public static readonly string CONFIG_KEY_TRUE = "true";

		/// <summary>
		/// The "always" key (used to configure
		/// <see cref="CONFIG_KEY_AUTOSETUPREBASE">CONFIG_KEY_AUTOSETUPREBASE</see>
		/// and
		/// <see cref="CONFIG_KEY_AUTOSETUPMERGE">CONFIG_KEY_AUTOSETUPMERGE</see>
		/// </summary>
		public static readonly string CONFIG_KEY_ALWAYS = "always";

		/// <summary>
		/// The "never" key (used to configure
		/// <see cref="CONFIG_KEY_AUTOSETUPREBASE">CONFIG_KEY_AUTOSETUPREBASE</see>
		/// 
		/// </summary>
		public static readonly string CONFIG_KEY_NEVER = "never";

		/// <summary>
		/// The "local" key (used to configure
		/// <see cref="CONFIG_KEY_AUTOSETUPREBASE">CONFIG_KEY_AUTOSETUPREBASE</see>
		/// 
		/// </summary>
		public static readonly string CONFIG_KEY_LOCAL = "local";

		/// <summary>The "createchangeid" key</summary>
		public static readonly string CONFIG_KEY_CREATECHANGEID = "createchangeid";

		/// <summary>The "defaultsourceref" key</summary>
		public static readonly string CONFIG_KEY_DEFBRANCHSTARTPOINT = "defbranchstartpoint";

		/// <summary>The "path" key</summary>
		public static readonly string CONFIG_KEY_PATH = "path";

		/// <summary>The "update" key</summary>
		public static readonly string CONFIG_KEY_UPDATE = "update";

		/// <summary>The "compression" key</summary>
		public static readonly string CONFIG_KEY_COMPRESSION = "compression";

		/// <summary>The "indexversion" key</summary>
		public static readonly string CONFIG_KEY_INDEXVERSION = "indexversion";
	}
}
