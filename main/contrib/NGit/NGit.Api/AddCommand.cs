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
using NGit.Api;
using NGit.Api.Errors;
using NGit.Dircache;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>Add</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command. Each instance of this class should only be
	/// used for one invocation of the command (means: one call to
	/// <see cref="Call()">Call()</see>
	/// )
	/// </summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-add.html"
	/// *      >Git documentation about Add</a></seealso>
	public class AddCommand : GitCommand<DirCache>
	{
		private ICollection<string> filepatterns;

		private WorkingTreeIterator workingTreeIterator;

		private bool update = false;

		/// <param name="repo"></param>
		protected internal AddCommand(Repository repo) : base(repo)
		{
			filepatterns = new List<string>();
		}

		/// <param name="filepattern">
		/// File to add content from. Also a leading directory name (e.g.
		/// dir to add dir/file1 and dir/file2) can be given to add all
		/// files in the directory, recursively. Fileglobs (e.g. *.c) are
		/// not yet supported.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.AddCommand AddFilepattern(string filepattern)
		{
			CheckCallable();
			filepatterns.AddItem(filepattern);
			return this;
		}

		/// <summary>Allow clients to provide their own implementation of a FileTreeIterator</summary>
		/// <param name="f"></param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.AddCommand SetWorkingTreeIterator(WorkingTreeIterator f)
		{
			workingTreeIterator = f;
			return this;
		}

		/// <summary>
		/// Executes the
		/// <code>Add</code>
		/// command. Each instance of this class should only
		/// be used for one invocation of the command. Don't call this method twice
		/// on an instance.
		/// </summary>
		/// <returns>the DirCache after Add</returns>
		/// <exception cref="NGit.Api.Errors.NoFilepatternException"></exception>
		public override DirCache Call()
		{
			if (filepatterns.IsEmpty())
			{
				throw new NoFilepatternException(JGitText.Get().atLeastOnePatternIsRequired);
			}
			CheckCallable();
			DirCache dc = null;
			bool addAll = false;
			if (filepatterns.Contains("."))
			{
				addAll = true;
			}
			ObjectInserter inserter = repo.NewObjectInserter();
			try
			{
				dc = repo.LockDirCache();
				DirCacheIterator c;
				DirCacheBuilder builder = dc.Builder();
				TreeWalk tw = new TreeWalk(repo);
				tw.AddTree(new DirCacheBuildIterator(builder));
				if (workingTreeIterator == null)
				{
					workingTreeIterator = new FileTreeIterator(repo);
				}
				tw.AddTree(workingTreeIterator);
				tw.Recursive = true;
				if (!addAll)
				{
					tw.Filter = PathFilterGroup.CreateFromStrings(filepatterns);
				}
				string lastAddedFile = null;
				while (tw.Next())
				{
					string path = tw.PathString;
					WorkingTreeIterator f = tw.GetTree<WorkingTreeIterator>(1);
					if (tw.GetTree<DirCacheIterator>(0) == null && f != null && f.IsEntryIgnored())
					{
					}
					else
					{
						// file is not in index but is ignored, do nothing
						// In case of an existing merge conflict the
						// DirCacheBuildIterator iterates over all stages of
						// this path, we however want to add only one
						// new DirCacheEntry per path.
						if (!(path.Equals(lastAddedFile)))
						{
							if (!(update && tw.GetTree<DirCacheIterator>(0) == null))
							{
								c = tw.GetTree<DirCacheIterator>(0);
								if (f != null)
								{
									// the file exists
									long sz = f.GetEntryLength();
									DirCacheEntry entry = new DirCacheEntry(path);
									if (c == null || c.GetDirCacheEntry() == null || !c.GetDirCacheEntry().IsAssumeValid)
									{
										entry.SetLength(sz);
										entry.LastModified = f.GetEntryLastModified();
										entry.FileMode = f.EntryFileMode;
										InputStream @in = f.OpenEntryStream();
										try
										{
											entry.SetObjectId(inserter.Insert(Constants.OBJ_BLOB, sz, @in));
										}
										finally
										{
											@in.Close();
										}
										builder.Add(entry);
										lastAddedFile = path;
									}
									else
									{
										builder.Add(c.GetDirCacheEntry());
									}
								}
								else
								{
									if (!update)
									{
										builder.Add(c.GetDirCacheEntry());
									}
								}
							}
						}
					}
				}
				inserter.Flush();
				builder.Commit();
				SetCallable(false);
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfAddCommand
					, e);
			}
			finally
			{
				inserter.Release();
				if (dc != null)
				{
					dc.Unlock();
				}
			}
			return dc;
		}

		/// <param name="update">
		/// If set to true, the command only matches
		/// <code>filepattern</code>
		/// against already tracked files in the index rather than the
		/// working tree. That means that it will never stage new files,
		/// but that it will stage modified new contents of tracked files
		/// and that it will remove files from the index if the
		/// corresponding files in the working tree have been removed.
		/// In contrast to the git command line a
		/// <code>filepattern</code>
		/// must
		/// exist also if update is set to true as there is no
		/// concept of a working directory here.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.AddCommand SetUpdate(bool update)
		{
			this.update = update;
			return this;
		}

		/// <returns>is the parameter update is set</returns>
		public virtual bool IsUpdate()
		{
			return update;
		}
	}
}
