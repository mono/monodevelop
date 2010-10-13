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
using System.IO;
using NGit;
using NGit.Errors;
using NGit.Storage.File;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// Cache of active
	/// <see cref="Repository">Repository</see>
	/// instances.
	/// </summary>
	public class RepositoryCache
	{
		private static readonly NGit.RepositoryCache cache = new NGit.RepositoryCache();

		/// <summary>Open an existing repository, reusing a cached instance if possible.</summary>
		/// <remarks>
		/// Open an existing repository, reusing a cached instance if possible.
		/// <p>
		/// When done with the repository, the caller must call
		/// <see cref="Repository.Close()">Repository.Close()</see>
		/// to decrement the repository's usage counter.
		/// </remarks>
		/// <param name="location">
		/// where the local repository is. Typically a
		/// <see cref="FileKey">FileKey</see>
		/// .
		/// </param>
		/// <returns>the repository instance requested; caller must close when done.</returns>
		/// <exception cref="System.IO.IOException">
		/// the repository could not be read (likely its core.version
		/// property is not supported).
		/// </exception>
		/// <exception cref="NGit.Errors.RepositoryNotFoundException">there is no repository at the given location.
		/// 	</exception>
		public static Repository Open(RepositoryCache.Key location)
		{
			return Open(location, true);
		}

		/// <summary>Open a repository, reusing a cached instance if possible.</summary>
		/// <remarks>
		/// Open a repository, reusing a cached instance if possible.
		/// <p>
		/// When done with the repository, the caller must call
		/// <see cref="Repository.Close()">Repository.Close()</see>
		/// to decrement the repository's usage counter.
		/// </remarks>
		/// <param name="location">
		/// where the local repository is. Typically a
		/// <see cref="FileKey">FileKey</see>
		/// .
		/// </param>
		/// <param name="mustExist">
		/// If true, and the repository is not found, throws
		/// <code>RepositoryNotFoundException</code>
		/// . If false, a repository instance
		/// is created and registered anyway.
		/// </param>
		/// <returns>the repository instance requested; caller must close when done.</returns>
		/// <exception cref="System.IO.IOException">
		/// the repository could not be read (likely its core.version
		/// property is not supported).
		/// </exception>
		/// <exception cref="NGit.Errors.RepositoryNotFoundException">
		/// There is no repository at the given location, only thrown if
		/// <code>mustExist</code>
		/// is true.
		/// </exception>
		public static Repository Open(RepositoryCache.Key location, bool mustExist)
		{
			return cache.OpenRepository(location, mustExist);
		}

		/// <summary>Register one repository into the cache.</summary>
		/// <remarks>
		/// Register one repository into the cache.
		/// <p>
		/// During registration the cache automatically increments the usage counter,
		/// permitting it to retain the reference. A
		/// <see cref="FileKey">FileKey</see>
		/// for the
		/// repository's
		/// <see cref="Repository.Directory()">Repository.Directory()</see>
		/// is used to index the
		/// repository in the cache.
		/// <p>
		/// If another repository already is registered in the cache at this
		/// location, the other instance is closed.
		/// </remarks>
		/// <param name="db">repository to register.</param>
		public static void Register(Repository db)
		{
			if (db.Directory != null)
			{
				RepositoryCache.FileKey key = RepositoryCache.FileKey.Exact(db.Directory, db.FileSystem
					);
				cache.RegisterRepository(key, db);
			}
		}

		/// <summary>Remove a repository from the cache.</summary>
		/// <remarks>
		/// Remove a repository from the cache.
		/// <p>
		/// Removes a repository from the cache, if it is still registered here,
		/// permitting it to close.
		/// </remarks>
		/// <param name="db">repository to unregister.</param>
		public static void Close(Repository db)
		{
			if (db.Directory != null)
			{
				RepositoryCache.FileKey key = RepositoryCache.FileKey.Exact(db.Directory, db.FileSystem
					);
				cache.UnregisterRepository(key);
			}
		}

		/// <summary>Unregister all repositories from the cache.</summary>
		/// <remarks>Unregister all repositories from the cache.</remarks>
		public static void Clear()
		{
			cache.ClearAll();
		}

		private readonly ConcurrentHashMap<RepositoryCache.Key, Reference<Repository>> cacheMap;

		private readonly RepositoryCache.Lock[] openLocks;

		public RepositoryCache()
		{
			cacheMap = new ConcurrentHashMap<RepositoryCache.Key, Reference<Repository>>();
			openLocks = new RepositoryCache.Lock[4];
			for (int i = 0; i < openLocks.Length; i++)
			{
				openLocks[i] = new RepositoryCache.Lock();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private Repository OpenRepository(RepositoryCache.Key location, bool mustExist)
		{
			Reference<Repository> @ref = cacheMap.Get(location);
			Repository db = @ref != null ? @ref.Get() : null;
			if (db == null)
			{
				lock (LockFor(location))
				{
					@ref = cacheMap.Get(location);
					db = @ref != null ? @ref.Get() : null;
					if (db == null)
					{
						db = location.Open(mustExist);
						@ref = new SoftReference<Repository>(db);
						cacheMap.Put(location, @ref);
					}
				}
			}
			db.IncrementOpen();
			return db;
		}

		private void RegisterRepository(RepositoryCache.Key location, Repository db)
		{
			db.IncrementOpen();
			SoftReference<Repository> newRef = new SoftReference<Repository>(db);
			Reference<Repository> oldRef = cacheMap.Put(location, newRef);
			Repository oldDb = oldRef != null ? oldRef.Get() : null;
			if (oldDb != null)
			{
				oldDb.Close();
			}
		}

		private void UnregisterRepository(RepositoryCache.Key location)
		{
			Reference<Repository> oldRef = Sharpen.Collections.Remove(cacheMap, location);
			Repository oldDb = oldRef != null ? oldRef.Get() : null;
			if (oldDb != null)
			{
				oldDb.Close();
			}
		}

		private void ClearAll()
		{
			for (int stage = 0; stage < 2; stage++)
			{
				for (Iterator<KeyValuePair<RepositoryCache.Key, Reference<Repository>>> i = cacheMap
					.EntrySet().Iterator(); i.HasNext(); )
				{
					KeyValuePair<RepositoryCache.Key, Reference<Repository>> e = i.Next();
					Repository db = e.Value.Get();
					if (db != null)
					{
						db.Close();
					}
					i.Remove();
				}
			}
		}

		private RepositoryCache.Lock LockFor(RepositoryCache.Key location)
		{
			return openLocks[((int)(((uint)location.GetHashCode()) >> 1)) % openLocks.Length];
		}

		private class Lock
		{
			// Used only for its monitor.
		}

		/// <summary>
		/// Abstract hash key for
		/// <see cref="RepositoryCache">RepositoryCache</see>
		/// entries.
		/// <p>
		/// A Key instance should be lightweight, and implement hashCode() and
		/// equals() such that two Key instances are equal if they represent the same
		/// Repository location.
		/// </summary>
		public interface Key
		{
			/// <summary>
			/// Called by
			/// <see cref="RepositoryCache.Open(Key)">RepositoryCache.Open(Key)</see>
			/// if it doesn't exist yet.
			/// <p>
			/// If a repository does not exist yet in the cache, the cache will call
			/// this method to acquire a handle to it.
			/// </summary>
			/// <param name="mustExist">
			/// true if the repository must exist in order to be opened;
			/// false if a new non-existent repository is permitted to be
			/// created (the caller is responsible for calling create).
			/// </param>
			/// <returns>the new repository instance.</returns>
			/// <exception cref="System.IO.IOException">
			/// the repository could not be read (likely its core.version
			/// property is not supported).
			/// </exception>
			/// <exception cref="NGit.Errors.RepositoryNotFoundException">
			/// There is no repository at the given location, only thrown
			/// if
			/// <code>mustExist</code>
			/// is true.
			/// </exception>
			Repository Open(bool mustExist);
		}

		/// <summary>Location of a Repository, using the standard java.io.File API.</summary>
		/// <remarks>Location of a Repository, using the standard java.io.File API.</remarks>
		public class FileKey : RepositoryCache.Key
		{
			/// <summary>Obtain a pointer to an exact location on disk.</summary>
			/// <remarks>
			/// Obtain a pointer to an exact location on disk.
			/// <p>
			/// No guessing is performed, the given location is exactly the GIT_DIR
			/// directory of the repository.
			/// </remarks>
			/// <param name="directory">location where the repository database is.</param>
			/// <param name="fs">
			/// the file system abstraction which will be necessary to
			/// perform certain file system operations.
			/// </param>
			/// <returns>a key for the given directory.</returns>
			/// <seealso cref="Lenient(Sharpen.FilePath, NGit.Util.FS)">Lenient(Sharpen.FilePath, NGit.Util.FS)
			/// 	</seealso>
			public static RepositoryCache.FileKey Exact(FilePath directory, FS fs)
			{
				return new RepositoryCache.FileKey(directory, fs);
			}

			/// <summary>Obtain a pointer to a location on disk.</summary>
			/// <remarks>
			/// Obtain a pointer to a location on disk.
			/// <p>
			/// The method performs some basic guessing to locate the repository.
			/// Searched paths are:
			/// <ol>
			/// <li>
			/// <code>directory</code>
			/// // assume exact match</li>
			/// <li>
			/// <code>directory</code>
			/// + "/.git" // assume working directory</li>
			/// <li>
			/// <code>directory</code>
			/// + ".git" // assume bare</li>
			/// </ol>
			/// </remarks>
			/// <param name="directory">location where the repository database might be.</param>
			/// <param name="fs">
			/// the file system abstraction which will be necessary to
			/// perform certain file system operations.
			/// </param>
			/// <returns>a key for the given directory.</returns>
			/// <seealso cref="Exact(Sharpen.FilePath, NGit.Util.FS)">Exact(Sharpen.FilePath, NGit.Util.FS)
			/// 	</seealso>
			public static RepositoryCache.FileKey Lenient(FilePath directory, FS fs)
			{
				FilePath gitdir = Resolve(directory, fs);
				return new RepositoryCache.FileKey(gitdir != null ? gitdir : directory, fs);
			}

			private readonly FilePath path;

			private readonly FS fs;

			/// <param name="directory">exact location of the repository.</param>
			/// <param name="fs">
			/// the file system abstraction which will be necessary to
			/// perform certain file system operations.
			/// </param>
			protected internal FileKey(FilePath directory, FS fs)
			{
				path = Canonical(directory);
				this.fs = fs;
			}

			private static FilePath Canonical(FilePath path)
			{
				try
				{
					return path.GetCanonicalFile();
				}
				catch (IOException)
				{
					return path.GetAbsoluteFile();
				}
			}

			/// <returns>location supplied to the constructor.</returns>
			public FilePath GetFile()
			{
				return path;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual Repository Open(bool mustExist)
			{
				if (mustExist && !IsGitRepository(path, fs))
				{
					throw new RepositoryNotFoundException(path);
				}
				return new FileRepository(path);
			}

			public override int GetHashCode()
			{
				return path.GetHashCode();
			}

			public override bool Equals(object o)
			{
				return o is RepositoryCache.FileKey && path.Equals(((RepositoryCache.FileKey)o).path
					);
			}

			public override string ToString()
			{
				return path.ToString();
			}

			/// <summary>Guess if a directory contains a Git repository.</summary>
			/// <remarks>
			/// Guess if a directory contains a Git repository.
			/// <p>
			/// This method guesses by looking for the existence of some key files
			/// and directories.
			/// </remarks>
			/// <param name="dir">the location of the directory to examine.</param>
			/// <param name="fs">
			/// the file system abstraction which will be necessary to
			/// perform certain file system operations.
			/// </param>
			/// <returns>
			/// true if the directory "looks like" a Git repository; false if
			/// it doesn't look enough like a Git directory to really be a
			/// Git directory.
			/// </returns>
			public static bool IsGitRepository(FilePath dir, FS fs)
			{
				return fs.Resolve(dir, "objects").Exists() && fs.Resolve(dir, "refs").Exists() &&
					 IsValidHead(new FilePath(dir, Constants.HEAD));
			}

			private static bool IsValidHead(FilePath head)
			{
				string @ref = ReadFirstLine(head);
				return @ref != null && (@ref.StartsWith("ref: refs/") || ObjectId.IsId(@ref));
			}

			private static string ReadFirstLine(FilePath head)
			{
				try
				{
					byte[] buf = IOUtil.ReadFully(head, 4096);
					int n = buf.Length;
					if (n == 0)
					{
						return null;
					}
					if (buf[n - 1] == '\n')
					{
						n--;
					}
					return RawParseUtils.Decode(buf, 0, n);
				}
				catch (IOException)
				{
					return null;
				}
			}

			/// <summary>Guess the proper path for a Git repository.</summary>
			/// <remarks>
			/// Guess the proper path for a Git repository.
			/// <p>
			/// The method performs some basic guessing to locate the repository.
			/// Searched paths are:
			/// <ol>
			/// <li>
			/// <code>directory</code>
			/// // assume exact match</li>
			/// <li>
			/// <code>directory</code>
			/// + "/.git" // assume working directory</li>
			/// <li>
			/// <code>directory</code>
			/// + ".git" // assume bare</li>
			/// </ol>
			/// </remarks>
			/// <param name="directory">location to guess from. Several permutations are tried.</param>
			/// <param name="fs">
			/// the file system abstraction which will be necessary to
			/// perform certain file system operations.
			/// </param>
			/// <returns>
			/// the actual directory location if a better match is found;
			/// null if there is no suitable match.
			/// </returns>
			public static FilePath Resolve(FilePath directory, FS fs)
			{
				if (IsGitRepository(directory, fs))
				{
					return directory;
				}
				if (IsGitRepository(new FilePath(directory, Constants.DOT_GIT), fs))
				{
					return new FilePath(directory, Constants.DOT_GIT);
				}
				string name = directory.GetName();
				FilePath parent = directory.GetParentFile();
				if (IsGitRepository(new FilePath(parent, name + Constants.DOT_GIT_EXT), fs))
				{
					return new FilePath(parent, name + Constants.DOT_GIT_EXT);
				}
				return null;
			}
		}
	}
}
