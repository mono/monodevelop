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

using System;
using System.Collections.Generic;
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>Base builder to customize repository construction.</summary>
	/// <remarks>
	/// Base builder to customize repository construction.
	/// <p>
	/// Repository implementations may subclass this builder in order to add custom
	/// repository detection methods.
	/// </remarks>
	/// <?></?>
	/// <?></?>
	/// <seealso cref="RepositoryBuilder">RepositoryBuilder</seealso>
	/// <seealso cref="NGit.Storage.File.FileRepositoryBuilder">NGit.Storage.File.FileRepositoryBuilder
	/// 	</seealso>
	public class BaseRepositoryBuilder<B, R> : BaseRepositoryBuilder where B: BaseRepositoryBuilder where R: Repository
	{
		private static bool IsSymRef(byte[] @ref)
		{
			if (@ref.Length < 9)
			{
				return false;
			}
			return @ref[0] == 'g' && @ref[1] == 'i' && @ref[2] == 't' && @ref[3] == 'd' && @ref
				[4] == 'i' && @ref[5] == 'r' && @ref[6] == ':' && @ref[7] == ' ';
		}

		private FS fs;

		private FilePath gitDir;

		private FilePath objectDirectory;

		private IList<FilePath> alternateObjectDirectories;

		private FilePath indexFile;

		private FilePath workTree;

		/// <summary>Directories limiting the search for a Git repository.</summary>
		/// <remarks>Directories limiting the search for a Git repository.</remarks>
		private IList<FilePath> ceilingDirectories;

		/// <summary>True only if the caller wants to force bare behavior.</summary>
		/// <remarks>True only if the caller wants to force bare behavior.</remarks>
		private bool bare;

		/// <summary>True if the caller requires the repository to exist.</summary>
		/// <remarks>True if the caller requires the repository to exist.</remarks>
		private bool mustExist;

		/// <summary>Configuration file of target repository, lazily loaded if required.</summary>
		/// <remarks>Configuration file of target repository, lazily loaded if required.</remarks>
		private Config config;

		//
		//
		//
		//
		//
		//
		//
		/// <summary>Set the file system abstraction needed by this repository.</summary>
		/// <remarks>Set the file system abstraction needed by this repository.</remarks>
		/// <param name="fs">the abstraction.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B SetFS(FS fs)
		{
			this.fs = fs;
			return Self();
		}

		/// <returns>the file system abstraction, or null if not set.</returns>
		public virtual FS GetFS()
		{
			return fs;
		}

		/// <summary>Set the Git directory storing the repository metadata.</summary>
		/// <remarks>
		/// Set the Git directory storing the repository metadata.
		/// <p>
		/// The meta directory stores the objects, references, and meta files like
		/// <code>MERGE_HEAD</code>
		/// , or the index file. If
		/// <code>null</code>
		/// the path is
		/// assumed to be
		/// <code>workTree/.git</code>
		/// .
		/// </remarks>
		/// <param name="gitDir">
		/// <code>GIT_DIR</code>
		/// , the repository meta directory.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B SetGitDir(FilePath gitDir)
		{
			this.gitDir = gitDir;
			this.config = null;
			return Self();
		}

		/// <returns>the meta data directory; null if not set.</returns>
		public virtual FilePath GetGitDir()
		{
			return gitDir;
		}

		/// <summary>Set the directory storing the repository's objects.</summary>
		/// <remarks>Set the directory storing the repository's objects.</remarks>
		/// <param name="objectDirectory">
		/// <code>GIT_OBJECT_DIRECTORY</code>
		/// , the directory where the
		/// repository's object files are stored.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B SetObjectDirectory(FilePath objectDirectory)
		{
			this.objectDirectory = objectDirectory;
			return Self();
		}

		/// <returns>the object directory; null if not set.</returns>
		public virtual FilePath GetObjectDirectory()
		{
			return objectDirectory;
		}

		/// <summary>Add an alternate object directory to the search list.</summary>
		/// <remarks>
		/// Add an alternate object directory to the search list.
		/// <p>
		/// This setting handles one alternate directory at a time, and is provided
		/// to support
		/// <code>GIT_ALTERNATE_OBJECT_DIRECTORIES</code>
		/// .
		/// </remarks>
		/// <param name="other">another objects directory to search after the standard one.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B AddAlternateObjectDirectory(FilePath other)
		{
			if (other != null)
			{
				if (alternateObjectDirectories == null)
				{
					alternateObjectDirectories = new List<FilePath>();
				}
				alternateObjectDirectories.AddItem(other);
			}
			return Self();
		}

		/// <summary>Add alternate object directories to the search list.</summary>
		/// <remarks>
		/// Add alternate object directories to the search list.
		/// <p>
		/// This setting handles several alternate directories at once, and is
		/// provided to support
		/// <code>GIT_ALTERNATE_OBJECT_DIRECTORIES</code>
		/// .
		/// </remarks>
		/// <param name="inList">
		/// other object directories to search after the standard one. The
		/// collection's contents is copied to an internal list.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B AddAlternateObjectDirectories(ICollection<FilePath> inList)
		{
			if (inList != null)
			{
				foreach (FilePath path in inList)
				{
					AddAlternateObjectDirectory(path);
				}
			}
			return Self();
		}

		/// <summary>Add alternate object directories to the search list.</summary>
		/// <remarks>
		/// Add alternate object directories to the search list.
		/// <p>
		/// This setting handles several alternate directories at once, and is
		/// provided to support
		/// <code>GIT_ALTERNATE_OBJECT_DIRECTORIES</code>
		/// .
		/// </remarks>
		/// <param name="inList">
		/// other object directories to search after the standard one. The
		/// array's contents is copied to an internal list.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B AddAlternateObjectDirectories(FilePath[] inList)
		{
			if (inList != null)
			{
				foreach (FilePath path in inList)
				{
					AddAlternateObjectDirectory(path);
				}
			}
			return Self();
		}

		/// <returns>ordered array of alternate directories; null if non were set.</returns>
		public virtual FilePath[] GetAlternateObjectDirectories()
		{
			IList<FilePath> alts = alternateObjectDirectories;
			if (alts == null)
			{
				return null;
			}
			return Sharpen.Collections.ToArray(alts, new FilePath[alts.Count]);
		}

		/// <summary>Force the repository to be treated as bare (have no working directory).</summary>
		/// <remarks>
		/// Force the repository to be treated as bare (have no working directory).
		/// <p>
		/// If bare the working directory aspects of the repository won't be
		/// configured, and will not be accessible.
		/// </remarks>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B SetBare()
		{
			SetIndexFile(null);
			SetWorkTree(null);
			bare = true;
			return Self();
		}

		/// <returns>
		/// true if this repository was forced bare by
		/// <see cref="BaseRepositoryBuilder{B, R}.SetBare()">BaseRepositoryBuilder&lt;B, R&gt;.SetBare()
		/// 	</see>
		/// .
		/// </returns>
		public virtual bool IsBare()
		{
			return bare;
		}

		/// <summary>Require the repository to exist before it can be opened.</summary>
		/// <remarks>Require the repository to exist before it can be opened.</remarks>
		/// <param name="mustExist">
		/// true if it must exist; false if it can be missing and created
		/// after being built.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B SetMustExist(bool mustExist)
		{
			this.mustExist = mustExist;
			return Self();
		}

		/// <returns>true if the repository must exist before being opened.</returns>
		public virtual bool IsMustExist()
		{
			return mustExist;
		}

		/// <summary>Set the top level directory of the working files.</summary>
		/// <remarks>Set the top level directory of the working files.</remarks>
		/// <param name="workTree">
		/// <code>GIT_WORK_TREE</code>
		/// , the working directory of the checkout.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B SetWorkTree(FilePath workTree)
		{
			this.workTree = workTree;
			return Self();
		}

		/// <returns>the work tree directory, or null if not set.</returns>
		public virtual FilePath GetWorkTree()
		{
			return workTree;
		}

		/// <summary>Set the local index file that is caching checked out file status.</summary>
		/// <remarks>
		/// Set the local index file that is caching checked out file status.
		/// <p>
		/// The location of the index file tracking the status information for each
		/// checked out file in
		/// <code>workTree</code>
		/// . This may be null to assume the
		/// default
		/// <code>gitDiir/index</code>
		/// .
		/// </remarks>
		/// <param name="indexFile">
		/// <code>GIT_INDEX_FILE</code>
		/// , the index file location.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B SetIndexFile(FilePath indexFile)
		{
			this.indexFile = indexFile;
			return Self();
		}

		/// <returns>the index file location, or null if not set.</returns>
		public virtual FilePath GetIndexFile()
		{
			return indexFile;
		}

		/// <summary>Read standard Git environment variables and configure from those.</summary>
		/// <remarks>
		/// Read standard Git environment variables and configure from those.
		/// <p>
		/// This method tries to read the standard Git environment variables, such as
		/// <code>GIT_DIR</code>
		/// and
		/// <code>GIT_WORK_TREE</code>
		/// to configure this builder
		/// instance. If an environment variable is set, it overrides the value
		/// already set in this builder.
		/// </remarks>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B ReadEnvironment()
		{
			return ReadEnvironment(SystemReader.GetInstance());
		}

		/// <summary>Read standard Git environment variables and configure from those.</summary>
		/// <remarks>
		/// Read standard Git environment variables and configure from those.
		/// <p>
		/// This method tries to read the standard Git environment variables, such as
		/// <code>GIT_DIR</code>
		/// and
		/// <code>GIT_WORK_TREE</code>
		/// to configure this builder
		/// instance. If a property is already set in the builder, the environment
		/// variable is not used.
		/// </remarks>
		/// <param name="sr">the SystemReader abstraction to access the environment.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B ReadEnvironment(SystemReader sr)
		{
			if (GetGitDir() == null)
			{
				string val = sr.Getenv(Constants.GIT_DIR_KEY);
				if (val != null)
				{
					SetGitDir(new FilePath(val));
				}
			}
			if (GetObjectDirectory() == null)
			{
				string val = sr.Getenv(Constants.GIT_OBJECT_DIRECTORY_KEY);
				if (val != null)
				{
					SetObjectDirectory(new FilePath(val));
				}
			}
			if (GetAlternateObjectDirectories() == null)
			{
				string val = sr.Getenv(Constants.GIT_ALTERNATE_OBJECT_DIRECTORIES_KEY);
				if (val != null)
				{
					foreach (string path in val.Split(FilePath.pathSeparator))
					{
						AddAlternateObjectDirectory(new FilePath(path));
					}
				}
			}
			if (GetWorkTree() == null)
			{
				string val = sr.Getenv(Constants.GIT_WORK_TREE_KEY);
				if (val != null)
				{
					SetWorkTree(new FilePath(val));
				}
			}
			if (GetIndexFile() == null)
			{
				string val = sr.Getenv(Constants.GIT_INDEX_FILE_KEY);
				if (val != null)
				{
					SetIndexFile(new FilePath(val));
				}
			}
			if (ceilingDirectories == null)
			{
				string val = sr.Getenv(Constants.GIT_CEILING_DIRECTORIES_KEY);
				if (val != null)
				{
					foreach (string path in val.Split(FilePath.pathSeparator))
					{
						AddCeilingDirectory(new FilePath(path));
					}
				}
			}
			return Self();
		}

		/// <summary>Add a ceiling directory to the search limit list.</summary>
		/// <remarks>
		/// Add a ceiling directory to the search limit list.
		/// <p>
		/// This setting handles one ceiling directory at a time, and is provided to
		/// support
		/// <code>GIT_CEILING_DIRECTORIES</code>
		/// .
		/// </remarks>
		/// <param name="root">a path to stop searching at; its parent will not be searched.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B AddCeilingDirectory(FilePath root)
		{
			if (root != null)
			{
				if (ceilingDirectories == null)
				{
					ceilingDirectories = new List<FilePath>();
				}
				ceilingDirectories.AddItem(root);
			}
			return Self();
		}

		/// <summary>Add ceiling directories to the search list.</summary>
		/// <remarks>
		/// Add ceiling directories to the search list.
		/// <p>
		/// This setting handles several ceiling directories at once, and is provided
		/// to support
		/// <code>GIT_CEILING_DIRECTORIES</code>
		/// .
		/// </remarks>
		/// <param name="inList">
		/// directory paths to stop searching at. The collection's
		/// contents is copied to an internal list.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B AddCeilingDirectories(ICollection<FilePath> inList)
		{
			if (inList != null)
			{
				foreach (FilePath path in inList)
				{
					AddCeilingDirectory(path);
				}
			}
			return Self();
		}

		/// <summary>Add ceiling directories to the search list.</summary>
		/// <remarks>
		/// Add ceiling directories to the search list.
		/// <p>
		/// This setting handles several ceiling directories at once, and is provided
		/// to support
		/// <code>GIT_CEILING_DIRECTORIES</code>
		/// .
		/// </remarks>
		/// <param name="inList">
		/// directory paths to stop searching at. The array's contents is
		/// copied to an internal list.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B AddCeilingDirectories(FilePath[] inList)
		{
			if (inList != null)
			{
				foreach (FilePath path in inList)
				{
					AddCeilingDirectory(path);
				}
			}
			return Self();
		}

		/// <summary>
		/// Configure
		/// <code>GIT_DIR</code>
		/// by searching up the file system.
		/// <p>
		/// Starts from the current working directory of the JVM and scans up through
		/// the directory tree until a Git repository is found. Success can be
		/// determined by checking for
		/// <code>getGitDir() != null</code>
		/// .
		/// <p>
		/// The search can be limited to specific spaces of the local filesystem by
		/// <see cref="BaseRepositoryBuilder{B, R}.AddCeilingDirectory(Sharpen.FilePath)">BaseRepositoryBuilder&lt;B, R&gt;.AddCeilingDirectory(Sharpen.FilePath)
		/// 	</see>
		/// , or inheriting the list through a
		/// prior call to
		/// <see cref="BaseRepositoryBuilder{B, R}.ReadEnvironment()">BaseRepositoryBuilder&lt;B, R&gt;.ReadEnvironment()
		/// 	</see>
		/// .
		/// </summary>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B FindGitDir()
		{
			if (GetGitDir() == null)
			{
				FindGitDir(new FilePath(string.Empty).GetAbsoluteFile());
			}
			return Self();
		}

		/// <summary>
		/// Configure
		/// <code>GIT_DIR</code>
		/// by searching up the file system.
		/// <p>
		/// Starts from the supplied directory path and scans up through the parent
		/// directory tree until a Git repository is found. Success can be determined
		/// by checking for
		/// <code>getGitDir() != null</code>
		/// .
		/// <p>
		/// The search can be limited to specific spaces of the local filesystem by
		/// <see cref="BaseRepositoryBuilder{B, R}.AddCeilingDirectory(Sharpen.FilePath)">BaseRepositoryBuilder&lt;B, R&gt;.AddCeilingDirectory(Sharpen.FilePath)
		/// 	</see>
		/// , or inheriting the list through a
		/// prior call to
		/// <see cref="BaseRepositoryBuilder{B, R}.ReadEnvironment()">BaseRepositoryBuilder&lt;B, R&gt;.ReadEnvironment()
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="current">directory to begin searching in.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// (for chaining calls).
		/// </returns>
		public virtual B FindGitDir(FilePath current)
		{
			if (GetGitDir() == null)
			{
				FS tryFS = SafeFS();
				while (current != null)
				{
					FilePath dir = new FilePath(current, Constants.DOT_GIT);
					if (RepositoryCache.FileKey.IsGitRepository(dir, tryFS))
					{
						SetGitDir(dir);
						break;
					}
					current = current.GetParentFile();
					if (current != null && ceilingDirectories != null && ceilingDirectories.Contains(
						current))
					{
						break;
					}
				}
			}
			return Self();
		}

		/// <summary>Guess and populate all parameters not already defined.</summary>
		/// <remarks>
		/// Guess and populate all parameters not already defined.
		/// <p>
		/// If an option was not set, the setup method will try to default the option
		/// based on other options. If insufficient information is available, an
		/// exception is thrown to the caller.
		/// </remarks>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="System.ArgumentException">
		/// insufficient parameters were set, or some parameters are
		/// incompatible with one another.
		/// </exception>
		/// <exception cref="System.IO.IOException">
		/// the repository could not be accessed to configure the rest of
		/// the builder's parameters.
		/// </exception>
		public virtual B Setup()
		{
			RequireGitDirOrWorkTree();
			SetupGitDir();
			SetupWorkTree();
			SetupInternals();
			return Self();
		}

		/// <summary>Create a repository matching the configuration in this builder.</summary>
		/// <remarks>
		/// Create a repository matching the configuration in this builder.
		/// <p>
		/// If an option was not set, the build method will try to default the option
		/// based on other options. If insufficient information is available, an
		/// exception is thrown to the caller.
		/// </remarks>
		/// <returns>a repository matching this configuration.</returns>
		/// <exception cref="System.ArgumentException">insufficient parameters were set.</exception>
		/// <exception cref="System.IO.IOException">
		/// the repository could not be accessed to configure the rest of
		/// the builder's parameters.
		/// </exception>
		public virtual R Build()
		{
			R repo = (R)(object)new FileRepository(Setup());
			if (IsMustExist() && !repo.ObjectDatabase.Exists())
			{
				throw new RepositoryNotFoundException(GetGitDir());
			}
			return repo;
		}

		/// <summary>
		/// Require either
		/// <code>gitDir</code>
		/// or
		/// <code>workTree</code>
		/// to be set.
		/// </summary>
		protected internal virtual void RequireGitDirOrWorkTree()
		{
			if (GetGitDir() == null && GetWorkTree() == null)
			{
				throw new ArgumentException(JGitText.Get().eitherGitDirOrWorkTreeRequired);
			}
		}

		/// <summary>Perform standard gitDir initialization.</summary>
		/// <remarks>Perform standard gitDir initialization.</remarks>
		/// <exception cref="System.IO.IOException">the repository could not be accessed</exception>
		protected internal virtual void SetupGitDir()
		{
			// No gitDir? Try to assume its under the workTree or a ref to another
			// location
			if (GetGitDir() == null && GetWorkTree() != null)
			{
				FilePath dotGit = new FilePath(GetWorkTree(), Constants.DOT_GIT);
				if (!dotGit.IsFile())
				{
					SetGitDir(dotGit);
				}
				else
				{
					byte[] content = IOUtil.ReadFully(dotGit);
					if (!IsSymRef(content))
					{
						throw new IOException(MessageFormat.Format(JGitText.Get().invalidGitdirRef, dotGit
							.GetAbsolutePath()));
					}
					int pathStart = 8;
					int lineEnd = RawParseUtils.NextLF(content, pathStart);
					if (content[lineEnd - 1] == '\n')
					{
						lineEnd--;
					}
					if (lineEnd == pathStart)
					{
						throw new IOException(MessageFormat.Format(JGitText.Get().invalidGitdirRef, dotGit
							.GetAbsolutePath()));
					}
					string gitdirPath = RawParseUtils.Decode(content, pathStart, lineEnd);
					FilePath gitdirFile = new FilePath(gitdirPath);
					if (gitdirFile.IsAbsolute())
					{
						SetGitDir(gitdirFile);
					}
					else
					{
						SetGitDir(new FilePath(GetWorkTree(), gitdirPath).GetCanonicalFile());
					}
				}
			}
		}

		/// <summary>Perform standard work-tree initialization.</summary>
		/// <remarks>
		/// Perform standard work-tree initialization.
		/// <p>
		/// This is a method typically invoked inside of
		/// <see cref="BaseRepositoryBuilder{B, R}.Setup()">BaseRepositoryBuilder&lt;B, R&gt;.Setup()
		/// 	</see>
		/// , near the
		/// end after the repository has been identified and its configuration is
		/// available for inspection.
		/// </remarks>
		/// <exception cref="System.IO.IOException">the repository configuration could not be read.
		/// 	</exception>
		protected internal virtual void SetupWorkTree()
		{
			if (GetFS() == null)
			{
				SetFS(FS.DETECTED);
			}
			// If we aren't bare, we should have a work tree.
			//
			if (!IsBare() && GetWorkTree() == null)
			{
				SetWorkTree(GuessWorkTreeOrFail());
			}
			if (!IsBare())
			{
				// If after guessing we're still not bare, we must have
				// a metadata directory to hold the repository. Assume
				// its at the work tree.
				//
				if (GetGitDir() == null)
				{
					SetGitDir(GetWorkTree().GetParentFile());
				}
				if (GetIndexFile() == null)
				{
					SetIndexFile(new FilePath(GetGitDir(), "index"));
				}
			}
		}

		/// <summary>Configure the internal implementation details of the repository.</summary>
		/// <remarks>Configure the internal implementation details of the repository.</remarks>
		/// <exception cref="System.IO.IOException">the repository could not be accessed</exception>
		protected internal virtual void SetupInternals()
		{
			if (GetObjectDirectory() == null && GetGitDir() != null)
			{
				SetObjectDirectory(SafeFS().Resolve(GetGitDir(), "objects"));
			}
		}

		/// <summary>Get the cached repository configuration, loading if not yet available.</summary>
		/// <remarks>Get the cached repository configuration, loading if not yet available.</remarks>
		/// <returns>the configuration of the repository.</returns>
		/// <exception cref="System.IO.IOException">the configuration is not available, or is badly formed.
		/// 	</exception>
		protected internal virtual Config GetConfig()
		{
			if (config == null)
			{
				config = LoadConfig();
			}
			return config;
		}

		/// <summary>Parse and load the repository specific configuration.</summary>
		/// <remarks>
		/// Parse and load the repository specific configuration.
		/// <p>
		/// The default implementation reads
		/// <code>gitDir/config</code>
		/// , or returns an
		/// empty configuration if gitDir was not set.
		/// </remarks>
		/// <returns>the repository's configuration.</returns>
		/// <exception cref="System.IO.IOException">the configuration is not available.</exception>
		protected internal virtual Config LoadConfig()
		{
			if (GetGitDir() != null)
			{
				// We only want the repository's configuration file, and not
				// the user file, as these parameters must be unique to this
				// repository and not inherited from other files.
				//
				FilePath path = SafeFS().Resolve(GetGitDir(), Constants.CONFIG);
				FileBasedConfig cfg = new FileBasedConfig(path, SafeFS());
				try
				{
					cfg.Load();
				}
				catch (ConfigInvalidException err)
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().repositoryConfigFileInvalid
						, path.GetAbsolutePath(), err.Message));
				}
				return cfg;
			}
			else
			{
				return new Config();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private FilePath GuessWorkTreeOrFail()
		{
			Config cfg = GetConfig();
			// If set, core.worktree wins.
			//
			string path = cfg.GetString(ConfigConstants.CONFIG_CORE_SECTION, null, ConfigConstants
				.CONFIG_KEY_WORKTREE);
			if (path != null)
			{
				return SafeFS().Resolve(GetGitDir(), path);
			}
			// If core.bare is set, honor its value. Assume workTree is
			// the parent directory of the repository.
			//
			if (cfg.GetString(ConfigConstants.CONFIG_CORE_SECTION, null, ConfigConstants.CONFIG_KEY_BARE
				) != null)
			{
				if (cfg.GetBoolean(ConfigConstants.CONFIG_CORE_SECTION, ConfigConstants.CONFIG_KEY_BARE
					, true))
				{
					SetBare();
					return null;
				}
				return GetGitDir().GetParentFile();
			}
			if (GetGitDir().GetName().Equals(Constants.DOT_GIT))
			{
				// No value for the "bare" flag, but gitDir is named ".git",
				// use the parent of the directory
				//
				return GetGitDir().GetParentFile();
			}
			// We have to assume we are bare.
			//
			SetBare();
			return null;
		}

		/// <returns>
		/// the configured FS, or
		/// <see cref="NGit.Util.FS.DETECTED">NGit.Util.FS.DETECTED</see>
		/// .
		/// </returns>
		protected internal virtual FS SafeFS()
		{
			return GetFS() != null ? GetFS() : FS.DETECTED;
		}

		/// <returns>
		/// 
		/// <code>this</code>
		/// 
		/// </returns>
		protected internal B Self()
		{
			return (B)(object)this;
		}
	}
	
	public interface BaseRepositoryBuilder
	{
	    // Methods
	    FilePath[] GetAlternateObjectDirectories();
	    FS GetFS();
	    FilePath GetGitDir();
	    FilePath GetIndexFile();
	    FilePath GetObjectDirectory();
	    FilePath GetWorkTree();
	}
	
	 
}
