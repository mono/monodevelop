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
using NGit.Events;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>Represents a Git repository.</summary>
	/// <remarks>
	/// Represents a Git repository. A repository holds all objects and refs used for
	/// managing source code (could by any type of file, but source code is what
	/// SCM's are typically used for).
	/// In Git terms all data is stored in GIT_DIR, typically a directory called
	/// .git. A work tree is maintained unless the repository is a bare repository.
	/// Typically the .git directory is located at the root of the work dir.
	/// <ul>
	/// <li>GIT_DIR
	/// <ul>
	/// <li>objects/ - objects</li>
	/// <li>refs/ - tags and heads</li>
	/// <li>config - configuration</li>
	/// <li>info/ - more configurations</li>
	/// </ul>
	/// </li>
	/// </ul>
	/// <p>
	/// This class is thread-safe.
	/// <p>
	/// This implementation only handles a subtly undocumented subset of git features.
	/// </remarks>
	public class FileRepository : Repository
	{
		private readonly FileBasedConfig systemConfig;

		private readonly FileBasedConfig userConfig;

		private readonly FileBasedConfig repoConfig;

		private readonly NGit.RefDatabase refs;

		private readonly ObjectDirectory objectDatabase;

		private FileSnapshot snapshot;

		/// <summary>Construct a representation of a Git repository.</summary>
		/// <remarks>
		/// Construct a representation of a Git repository.
		/// <p>
		/// The work tree, object directory, alternate object directories and index
		/// file locations are deduced from the given git directory and the default
		/// rules by running
		/// <see cref="FileRepositoryBuilder">FileRepositoryBuilder</see>
		/// . This constructor is the
		/// same as saying:
		/// <pre>
		/// new FileRepositoryBuilder().setGitDir(gitDir).build()
		/// </pre>
		/// </remarks>
		/// <param name="gitDir">GIT_DIR (the location of the repository metadata).</param>
		/// <exception cref="System.IO.IOException">
		/// the repository appears to already exist but cannot be
		/// accessed.
		/// </exception>
		/// <seealso cref="FileRepositoryBuilder">FileRepositoryBuilder</seealso>
		public FileRepository(FilePath gitDir) : this(new FileRepositoryBuilder().SetGitDir
			(gitDir).Setup())
		{
		}

		/// <summary>
		/// A convenience API for
		/// <see cref="FileRepository(Sharpen.FilePath)">FileRepository(Sharpen.FilePath)</see>
		/// .
		/// </summary>
		/// <param name="gitDir">GIT_DIR (the location of the repository metadata).</param>
		/// <exception cref="System.IO.IOException">
		/// the repository appears to already exist but cannot be
		/// accessed.
		/// </exception>
		/// <seealso cref="FileRepositoryBuilder">FileRepositoryBuilder</seealso>
		public FileRepository(string gitDir) : this(new FilePath(gitDir))
		{
		}

		/// <summary>Create a repository using the local file system.</summary>
		/// <remarks>Create a repository using the local file system.</remarks>
		/// <param name="options">description of the repository's important paths.</param>
		/// <exception cref="System.IO.IOException">
		/// the user configuration file or repository configuration file
		/// cannot be accessed.
		/// </exception>
		protected internal FileRepository(BaseRepositoryBuilder options) : base(options)
		{
			systemConfig = SystemReader.GetInstance().OpenSystemConfig(null, FileSystem);
			userConfig = SystemReader.GetInstance().OpenUserConfig(systemConfig, FileSystem);
			repoConfig = new FileBasedConfig(userConfig, FileSystem.Resolve(Directory, Constants
				.CONFIG), FileSystem);
			LoadSystemConfig();
			LoadUserConfig();
			LoadRepoConfig();
			repoConfig.AddChangeListener(new _ConfigChangedListener_171(this));
			refs = new RefDirectory(this);
			objectDatabase = new ObjectDirectory(repoConfig, options.GetObjectDirectory(), options
				.GetAlternateObjectDirectories(), FileSystem);
			//
			//
			//
			if (objectDatabase.Exists())
			{
				string repositoryFormatVersion = ((FileBasedConfig)GetConfig()).GetString(ConfigConstants
					.CONFIG_CORE_SECTION, null, ConfigConstants.CONFIG_KEY_REPO_FORMAT_VERSION);
				if (!string.IsNullOrEmpty (repositoryFormatVersion) && !"0".Equals(repositoryFormatVersion))
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().unknownRepositoryFormat2
						, repositoryFormatVersion));
				}
			}
			if (!IsBare)
			{
				snapshot = FileSnapshot.Save(GetIndexFile());
			}
		}

		private sealed class _ConfigChangedListener_171 : ConfigChangedListener
		{
			public _ConfigChangedListener_171(FileRepository _enclosing)
			{
				this._enclosing = _enclosing;
			}

			public void OnConfigChanged(ConfigChangedEvent @event)
			{
				this._enclosing.FireEvent(@event);
			}

			private readonly FileRepository _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void LoadSystemConfig()
		{
			try
			{
				systemConfig.Load();
			}
			catch (ConfigInvalidException e1)
			{
				IOException e2 = new IOException(MessageFormat.Format(JGitText.Get().systemConfigFileInvalid
					, systemConfig.GetFile().GetAbsolutePath(), e1));
				Sharpen.Extensions.InitCause(e2, e1);
				throw e2;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void LoadUserConfig()
		{
			try
			{
				userConfig.Load();
			}
			catch (ConfigInvalidException e1)
			{
				IOException e2 = new IOException(MessageFormat.Format(JGitText.Get().userConfigFileInvalid
					, userConfig.GetFile().GetAbsolutePath(), e1));
				Sharpen.Extensions.InitCause(e2, e1);
				throw e2;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void LoadRepoConfig()
		{
			try
			{
				repoConfig.Load();
			}
			catch (ConfigInvalidException e1)
			{
				IOException e2 = new IOException(JGitText.Get().unknownRepositoryFormat);
				Sharpen.Extensions.InitCause(e2, e1);
				throw e2;
			}
		}

		/// <summary>
		/// Create a new Git repository initializing the necessary files and
		/// directories.
		/// </summary>
		/// <remarks>
		/// Create a new Git repository initializing the necessary files and
		/// directories.
		/// </remarks>
		/// <param name="bare">if true, a bare repository is created.</param>
		/// <exception cref="System.IO.IOException">in case of IO problem</exception>
		public override void Create(bool bare)
		{
			FileBasedConfig cfg = ((FileBasedConfig)GetConfig());
			if (cfg.GetFile().Exists())
			{
				throw new InvalidOperationException(MessageFormat.Format(JGitText.Get().repositoryAlreadyExists
					, Directory));
			}
			FileUtils.Mkdirs(Directory, true);
			refs.Create();
			objectDatabase.Create();
			FileUtils.Mkdir(new FilePath(Directory, "branches"));
			FileUtils.Mkdir(new FilePath(Directory, "hooks"));
			RefUpdate head = UpdateRef(Constants.HEAD);
			head.DisableRefLog();
			head.Link(Constants.R_HEADS + Constants.MASTER);
			bool fileMode;
			if (FileSystem.SupportsExecute())
			{
				FilePath tmp = FilePath.CreateTempFile("try", "execute", Directory);
				FileSystem.SetExecute(tmp, true);
				bool on = FileSystem.CanExecute(tmp);
				FileSystem.SetExecute(tmp, false);
				bool off = FileSystem.CanExecute(tmp);
				FileUtils.Delete(tmp);
				fileMode = on && !off;
			}
			else
			{
				fileMode = false;
			}
			cfg.SetInt(ConfigConstants.CONFIG_CORE_SECTION, null, ConfigConstants.CONFIG_KEY_REPO_FORMAT_VERSION
				, 0);
			cfg.SetBoolean(ConfigConstants.CONFIG_CORE_SECTION, null, ConfigConstants.CONFIG_KEY_FILEMODE
				, fileMode);
			if (bare)
			{
				cfg.SetBoolean(ConfigConstants.CONFIG_CORE_SECTION, null, ConfigConstants.CONFIG_KEY_BARE
					, true);
			}
			cfg.SetBoolean(ConfigConstants.CONFIG_CORE_SECTION, null, ConfigConstants.CONFIG_KEY_LOGALLREFUPDATES
				, !bare);
			cfg.SetBoolean(ConfigConstants.CONFIG_CORE_SECTION, null, ConfigConstants.CONFIG_KEY_AUTOCRLF
				, false);
			cfg.Save();
		}

		/// <returns>the directory containing the objects owned by this repository.</returns>
		public virtual FilePath ObjectsDirectory
		{
			get
			{
				return objectDatabase.GetDirectory();
			}
		}

		/// <returns>the object database which stores this repository's data.</returns>
		public override NGit.ObjectDatabase ObjectDatabase
		{
			get
			{
				return objectDatabase;
			}
		}

		/// <returns>the reference database which stores the reference namespace.</returns>
		public override NGit.RefDatabase RefDatabase
		{
			get
			{
				return refs;
			}
		}

		/// <returns>the configuration of this repository</returns>
		public override StoredConfig GetConfig()
		{
			if (systemConfig.IsOutdated())
			{
				try
				{
					LoadSystemConfig();
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
			}
			if (userConfig.IsOutdated())
			{
				try
				{
					LoadUserConfig();
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
			}
			if (repoConfig.IsOutdated())
			{
				try
				{
					LoadRepoConfig();
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
			}
			return repoConfig;
		}

		/// <summary>
		/// Objects known to exist but not expressed by
		/// <see cref="NGit.Repository.GetAllRefs()">NGit.Repository.GetAllRefs()</see>
		/// .
		/// <p>
		/// When a repository borrows objects from another repository, it can
		/// advertise that it safely has that other repository's references, without
		/// exposing any other details about the other repository.  This may help
		/// a client trying to push changes avoid pushing more than it needs to.
		/// </summary>
		/// <returns>unmodifiable collection of other known objects.</returns>
		public override ICollection<ObjectId> GetAdditionalHaves()
		{
			HashSet<ObjectId> r = new HashSet<ObjectId>();
			foreach (FileObjectDatabase.AlternateHandle d in objectDatabase.MyAlternates())
			{
				if (d is FileObjectDatabase.AlternateRepository)
				{
					Repository repo;
					repo = ((FileObjectDatabase.AlternateRepository)d).repository;
					foreach (Ref @ref in repo.GetAllRefs().Values)
					{
						if (@ref.GetObjectId() != null)
						{
							r.AddItem(@ref.GetObjectId());
						}
						if (@ref.GetPeeledObjectId() != null)
						{
							r.AddItem(@ref.GetPeeledObjectId());
						}
					}
					Sharpen.Collections.AddAll(r, repo.GetAdditionalHaves());
				}
			}
			return r;
		}

		/// <summary>Add a single existing pack to the list of available pack files.</summary>
		/// <remarks>Add a single existing pack to the list of available pack files.</remarks>
		/// <param name="pack">path of the pack file to open.</param>
		/// <param name="idx">path of the corresponding index file.</param>
		/// <exception cref="System.IO.IOException">
		/// index file could not be opened, read, or is not recognized as
		/// a Git pack file index.
		/// </exception>
		public virtual void OpenPack(FilePath pack, FilePath idx)
		{
			objectDatabase.OpenPack(pack, idx);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void ScanForRepoChanges()
		{
			GetAllRefs();
			// This will look for changes to refs
			DetectIndexChanges();
		}

		/// <summary>Detect index changes.</summary>
		/// <remarks>Detect index changes.</remarks>
		private void DetectIndexChanges()
		{
			if (IsBare)
			{
				return;
			}
			FilePath indexFile = GetIndexFile();
			if (snapshot == null)
			{
				snapshot = FileSnapshot.Save(indexFile);
			}
			else
			{
				if (snapshot.IsModified(indexFile))
				{
					NotifyIndexChanged();
				}
			}
		}

		public override void NotifyIndexChanged()
		{
			snapshot = FileSnapshot.Save(GetIndexFile());
			FireEvent(new IndexChangedEvent());
		}

		/// <param name="refName"></param>
		/// <returns>
		/// a
		/// <see cref="ReflogReader">ReflogReader</see>
		/// for the supplied refname, or null if the
		/// named ref does not exist.
		/// </returns>
		/// <exception cref="System.IO.IOException">the ref could not be accessed.</exception>
		public override ReflogReader GetReflogReader(string refName)
		{
			Ref @ref = GetRef(refName);
			if (@ref != null)
			{
				return new ReflogReader(this, @ref.GetName());
			}
			return null;
		}
	}
}
