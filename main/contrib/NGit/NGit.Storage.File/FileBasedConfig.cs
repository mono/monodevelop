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

using System.IO;
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit.Storage.File
{
	/// <summary>The configuration file that is stored in the file of the file system.</summary>
	/// <remarks>The configuration file that is stored in the file of the file system.</remarks>
	public class FileBasedConfig : StoredConfig
	{
		private readonly FilePath configFile;

		private readonly FS fs;

		private volatile FileSnapshot snapshot;

		private volatile ObjectId hash;

		/// <summary>Create a configuration with no default fallback.</summary>
		/// <remarks>Create a configuration with no default fallback.</remarks>
		/// <param name="cfgLocation">the location of the configuration file on the file system
		/// 	</param>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to perform
		/// certain file system operations.
		/// </param>
		public FileBasedConfig(FilePath cfgLocation, FS fs) : this(null, cfgLocation, fs)
		{
		}

		/// <summary>The constructor</summary>
		/// <param name="base">the base configuration file</param>
		/// <param name="cfgLocation">the location of the configuration file on the file system
		/// 	</param>
		/// <param name="fs">
		/// the file system abstraction which will be necessary to perform
		/// certain file system operations.
		/// </param>
		public FileBasedConfig(Config @base, FilePath cfgLocation, FS fs) : base(@base)
		{
			configFile = cfgLocation;
			this.fs = fs;
			this.snapshot = FileSnapshot.DIRTY;
			this.hash = ObjectId.ZeroId;
		}

		protected internal override bool NotifyUponTransientChanges()
		{
			// we will notify listeners upon save()
			return false;
		}

		/// <returns>location of the configuration file on disk</returns>
		public FilePath GetFile()
		{
			return configFile;
		}

		/// <summary>Load the configuration as a Git text style configuration file.</summary>
		/// <remarks>
		/// Load the configuration as a Git text style configuration file.
		/// <p>
		/// If the file does not exist, this configuration is cleared, and thus
		/// behaves the same as though the file exists, but is empty.
		/// </remarks>
		/// <exception cref="System.IO.IOException">the file could not be read (but does exist).
		/// 	</exception>
		/// <exception cref="NGit.Errors.ConfigInvalidException">the file is not a properly formatted configuration file.
		/// 	</exception>
		public override void Load()
		{
			FileSnapshot oldSnapshot = snapshot;
			FileSnapshot newSnapshot = FileSnapshot.Save(GetFile());
			try
			{
				byte[] @in = IOUtil.ReadFully(GetFile());
				ObjectId newHash = Hash(@in);
				if (hash.Equals(newHash))
				{
					if (oldSnapshot.Equals(newSnapshot))
					{
						oldSnapshot.SetClean(newSnapshot);
					}
					else
					{
						snapshot = newSnapshot;
					}
				}
				else
				{
					FromText(RawParseUtils.Decode(@in));
					snapshot = newSnapshot;
					hash = newHash;
				}
			}
			catch (FileNotFoundException)
			{
				Clear();
				snapshot = newSnapshot;
			}
			catch (IOException e)
			{
				IOException e2 = new IOException(MessageFormat.Format(JGitText.Get().cannotReadFile
					, GetFile()));
				Sharpen.Extensions.InitCause(e2, e);
				throw e2;
			}
			catch (ConfigInvalidException e)
			{
				throw new ConfigInvalidException(MessageFormat.Format(JGitText.Get().cannotReadFile
					, GetFile()), e);
			}
		}

		/// <summary>Save the configuration as a Git text style configuration file.</summary>
		/// <remarks>
		/// Save the configuration as a Git text style configuration file.
		/// <p>
		/// <b>Warning:</b> Although this method uses the traditional Git file
		/// locking approach to protect against concurrent writes of the
		/// configuration file, it does not ensure that the file has not been
		/// modified since the last read, which means updates performed by other
		/// objects accessing the same backing file may be lost.
		/// </remarks>
		/// <exception cref="System.IO.IOException">the file could not be written.</exception>
		public override void Save()
		{
			byte[] @out = Constants.Encode(ToText());
			LockFile lf = new LockFile(GetFile(), fs);
			if (!lf.Lock())
			{
				throw new LockFailedException(GetFile());
			}
			try
			{
				lf.SetNeedSnapshot(true);
				lf.Write(@out);
				if (!lf.Commit())
				{
					throw new IOException(MessageFormat.Format(JGitText.Get().cannotCommitWriteTo, GetFile
						()));
				}
			}
			finally
			{
				lf.Unlock();
			}
			snapshot = lf.GetCommitSnapshot();
			hash = Hash(@out);
			// notify the listeners
			FireConfigChangedEvent();
		}

		protected internal override void Clear()
		{
			hash = Hash(new byte[0]);
			base.Clear();
		}

		private static ObjectId Hash(byte[] rawText)
		{
			return ObjectId.FromRaw(Constants.NewMessageDigest().Digest(rawText));
		}

		public override string ToString()
		{
			return GetType().Name + "[" + GetFile().GetPath() + "]";
		}

		/// <returns>
		/// returns true if the currently loaded configuration file is older
		/// than the file on disk
		/// </returns>
		public virtual bool IsOutdated()
		{
			return snapshot.IsModified(GetFile());
		}
	}
}
