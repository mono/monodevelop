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
using NGit.Internal;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>Rm</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command. Each instance of this class should only be
	/// used for one invocation of the command (means: one call to
	/// <see cref="Call()">Call()</see>
	/// )
	/// </summary>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-rm.html"
	/// *      >Git documentation about Rm</a></seealso>
	public class RmCommand : GitCommand<DirCache>
	{
		private ICollection<string> filepatterns;

		/// <param name="repo"></param>
		protected internal RmCommand(Repository repo) : base(repo)
		{
			filepatterns = new List<string>();
		}

		/// <param name="filepattern">File to remove.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.RmCommand AddFilepattern(string filepattern)
		{
			CheckCallable();
			filepatterns.AddItem(filepattern);
			return this;
		}

		/// <summary>
		/// Executes the
		/// <code>Rm</code>
		/// command. Each instance of this class should only
		/// be used for one invocation of the command. Don't call this method twice
		/// on an instance.
		/// </summary>
		/// <returns>the DirCache after Rm</returns>
		/// <exception cref="NGit.Api.Errors.NoFilepatternException"></exception>
		public override DirCache Call()
		{
			if (filepatterns.IsEmpty())
			{
				throw new NoFilepatternException(JGitText.Get().atLeastOnePatternIsRequired);
			}
			CheckCallable();
			DirCache dc = null;
			try
			{
				dc = repo.LockDirCache();
				DirCacheBuilder builder = dc.Builder();
				TreeWalk tw = new TreeWalk(repo);
				tw.Reset();
				// drop the first empty tree, which we do not need here
				tw.Recursive = true;
				tw.Filter = PathFilterGroup.CreateFromStrings(filepatterns);
				tw.AddTree(new DirCacheBuildIterator(builder));
				while (tw.Next())
				{
					FilePath path = new FilePath(repo.WorkTree, tw.PathString);
					FileMode mode = tw.GetFileMode(0);
					if (mode.GetObjectType() == Constants.OBJ_BLOB)
					{
						// Deleting a blob is simply a matter of removing
						// the file or symlink named by the tree entry.
						Delete(path);
					}
				}
				builder.Commit();
				SetCallable(false);
			}
			catch (IOException e)
			{
				throw new JGitInternalException(JGitText.Get().exceptionCaughtDuringExecutionOfRmCommand
					, e);
			}
			finally
			{
				if (dc != null)
				{
					dc.Unlock();
				}
			}
			return dc;
		}

		private void Delete(FilePath p)
		{
			while (p != null && !p.Equals(repo.WorkTree) && p.Delete())
			{
				p = p.GetParentFile();
			}
		}
	}
}
