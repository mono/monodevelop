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
using NGit.Transport.Resolver;
using NGit.Util;
using Sharpen;

namespace NGit.Transport.Resolver
{
	/// <summary>Default resolver serving from the local filesystem.</summary>
	/// <remarks>Default resolver serving from the local filesystem.</remarks>
	/// <?></?>
	public class FileResolver<C> : RepositoryResolver<C>
	{
		private volatile bool exportAll;

		private readonly IDictionary<string, Repository> exports;

		private readonly ICollection<FilePath> exportBase;

		/// <summary>Initialize an empty file based resolver.</summary>
		/// <remarks>Initialize an empty file based resolver.</remarks>
		public FileResolver()
		{
			exports = new ConcurrentHashMap<string, Repository>();
			exportBase = new CopyOnWriteArrayList<FilePath>();
		}

		/// <summary>Create a new resolver for the given path.</summary>
		/// <remarks>Create a new resolver for the given path.</remarks>
		/// <param name="basePath">the base path all repositories are rooted under.</param>
		/// <param name="exportAll">
		/// if true, exports all repositories, ignoring the check for the
		/// <code>git-daemon-export-ok</code>
		/// files.
		/// </param>
		public FileResolver(FilePath basePath, bool exportAll) : this()
		{
			ExportDirectory(basePath);
			SetExportAll(exportAll);
		}

		/// <exception cref="NGit.Errors.RepositoryNotFoundException"></exception>
		/// <exception cref="NGit.Transport.Resolver.ServiceNotEnabledException"></exception>
		public override Repository Open(C req, string name)
		{
			if (IsUnreasonableName(name))
			{
				throw new RepositoryNotFoundException(name);
			}
			Repository db = exports.Get(NameWithDotGit(name));
			if (db != null)
			{
				db.IncrementOpen();
				return db;
			}
			foreach (FilePath @base in exportBase)
			{
				FilePath dir = RepositoryCache.FileKey.Resolve(new FilePath(@base, name), FS.DETECTED
					);
				if (dir == null)
				{
					continue;
				}
				try
				{
					RepositoryCache.FileKey key = RepositoryCache.FileKey.Exact(dir, FS.DETECTED);
					db = RepositoryCache.Open(key, true);
				}
				catch (IOException e)
				{
					throw new RepositoryNotFoundException(name, e);
				}
				try
				{
					if (IsExportOk(req, name, db))
					{
						// We have to leak the open count to the caller, they
						// are responsible for closing the repository if we
						// complete successfully.
						return db;
					}
					else
					{
						throw new ServiceNotEnabledException();
					}
				}
				catch (RuntimeException e)
				{
					db.Close();
					throw new RepositoryNotFoundException(name, e);
				}
				catch (IOException e)
				{
					db.Close();
					throw new RepositoryNotFoundException(name, e);
				}
				catch (ServiceNotEnabledException e)
				{
					db.Close();
					throw;
				}
			}
			if (exportBase.Count == 1)
			{
				FilePath dir = new FilePath(exportBase.Iterator().Next(), name);
				throw new RepositoryNotFoundException(name, new RepositoryNotFoundException(dir));
			}
			throw new RepositoryNotFoundException(name);
		}

		/// <returns>
		/// false if <code>git-daemon-export-ok</code> is required to export
		/// a repository; true if <code>git-daemon-export-ok</code> is
		/// ignored.
		/// </returns>
		/// <seealso cref="FileResolver{C}.SetExportAll(bool)">FileResolver&lt;C&gt;.SetExportAll(bool)
		/// 	</seealso>
		public virtual bool IsExportAll()
		{
			return exportAll;
		}

		/// <summary>Set whether or not to export all repositories.</summary>
		/// <remarks>
		/// Set whether or not to export all repositories.
		/// <p>
		/// If false (the default), repositories must have a
		/// <code>git-daemon-export-ok</code> file to be accessed through this
		/// daemon.
		/// <p>
		/// If true, all repositories are available through the daemon, whether or
		/// not <code>git-daemon-export-ok</code> exists.
		/// </remarks>
		/// <param name="export"></param>
		public virtual void SetExportAll(bool export)
		{
			exportAll = export;
		}

		/// <summary>Add a single repository to the set that is exported by this daemon.</summary>
		/// <remarks>
		/// Add a single repository to the set that is exported by this daemon.
		/// <p>
		/// The existence (or lack-thereof) of <code>git-daemon-export-ok</code> is
		/// ignored by this method. The repository is always published.
		/// </remarks>
		/// <param name="name">name the repository will be published under.</param>
		/// <param name="db">the repository instance.</param>
		public virtual void ExportRepository(string name, Repository db)
		{
			exports.Put(NameWithDotGit(name), db);
		}

		/// <summary>Recursively export all Git repositories within a directory.</summary>
		/// <remarks>Recursively export all Git repositories within a directory.</remarks>
		/// <param name="dir">
		/// the directory to export. This directory must not itself be a
		/// git repository, but any directory below it which has a file
		/// named <code>git-daemon-export-ok</code> will be published.
		/// </param>
		public virtual void ExportDirectory(FilePath dir)
		{
			exportBase.AddItem(dir);
		}

		/// <summary>Check if this repository can be served.</summary>
		/// <remarks>
		/// Check if this repository can be served.
		/// <p>
		/// The default implementation of this method returns true only if either
		/// <see cref="FileResolver{C}.IsExportAll()">FileResolver&lt;C&gt;.IsExportAll()</see>
		/// is true, or the
		/// <code>git-daemon-export-ok</code>
		/// file
		/// is present in the repository's directory.
		/// </remarks>
		/// <param name="req">the current HTTP request.</param>
		/// <param name="repositoryName">name of the repository, as present in the URL.</param>
		/// <param name="db">the opened repository instance.</param>
		/// <returns>true if the repository is accessible; false if not.</returns>
		/// <exception cref="System.IO.IOException">
		/// the repository could not be accessed, the caller will claim
		/// the repository does not exist.
		/// </exception>
		protected internal virtual bool IsExportOk(C req, string repositoryName, Repository
			 db)
		{
			if (IsExportAll())
			{
				return true;
			}
			else
			{
				if (db.Directory != null)
				{
					return new FilePath(db.Directory, "git-daemon-export-ok").Exists();
				}
				else
				{
					return false;
				}
			}
		}

		private static string NameWithDotGit(string name)
		{
			if (name.EndsWith(Constants.DOT_GIT_EXT))
			{
				return name;
			}
			return name + Constants.DOT_GIT_EXT;
		}

		private static bool IsUnreasonableName(string name)
		{
			if (name.Length == 0)
			{
				return true;
			}
			// no empty paths
			if (name.IndexOf('\\') >= 0)
			{
				return true;
			}
			// no windows/dos style paths
			if (new FilePath(name).IsAbsolute())
			{
				return true;
			}
			// no absolute paths
			if (name.StartsWith("../"))
			{
				return true;
			}
			// no "l../etc/passwd"
			if (name.Contains("/../"))
			{
				return true;
			}
			// no "foo/../etc/passwd"
			if (name.Contains("/./"))
			{
				return true;
			}
			// "foo/./foo" is insane to ask
			if (name.Contains("//"))
			{
				return true;
			}
			// double slashes is sloppy, don't use it
			return false;
		}
		// is a reasonable name
	}
}
