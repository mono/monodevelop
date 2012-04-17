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
using NGit;
using NGit.Api;
using NGit.Api.Errors;
using NGit.Diff;
using NGit.Dircache;
using NGit.Internal;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;

namespace NGit.Api
{
	/// <summary>Show changes between commits, commit and working tree, etc.</summary>
	/// <remarks>Show changes between commits, commit and working tree, etc.</remarks>
	/// <seealso><a href="http://www.kernel.org/pub/software/scm/git/docs/git-diff.html"
	/// *      >Git documentation about diff</a></seealso>
	public class DiffCommand : GitCommand<IList<DiffEntry>>
	{
		private AbstractTreeIterator oldTree;

		private AbstractTreeIterator newTree;

		private bool cached;

		private TreeFilter pathFilter = TreeFilter.ALL;

		private bool showNameAndStatusOnly;

		private OutputStream @out;

		private int contextLines = -1;

		private string sourcePrefix;

		private string destinationPrefix;

		private ProgressMonitor monitor = NullProgressMonitor.INSTANCE;

		/// <param name="repo"></param>
		protected internal DiffCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>
		/// Executes the
		/// <code>Diff</code>
		/// command with all the options and parameters
		/// collected by the setter methods (e.g.
		/// <see cref="SetCached(bool)">SetCached(bool)</see>
		/// of this
		/// class. Each instance of this class should only be used for one invocation
		/// of the command. Don't call this method twice on an instance.
		/// </summary>
		/// <returns>a DiffEntry for each path which is different</returns>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override IList<DiffEntry> Call()
		{
			DiffFormatter diffFmt = new DiffFormatter(new BufferedOutputStream(@out));
			diffFmt.SetRepository(repo);
			diffFmt.SetProgressMonitor(monitor);
			try
			{
				if (cached)
				{
					if (oldTree == null)
					{
						ObjectId head = repo.Resolve(Constants.HEAD + "^{tree}");
						if (head == null)
						{
							throw new NoHeadException(JGitText.Get().cannotReadTree);
						}
						CanonicalTreeParser p = new CanonicalTreeParser();
						ObjectReader reader = repo.NewObjectReader();
						try
						{
							p.Reset(reader, head);
						}
						finally
						{
							reader.Release();
						}
						oldTree = p;
					}
					newTree = new DirCacheIterator(repo.ReadDirCache());
				}
				else
				{
					if (oldTree == null)
					{
						oldTree = new DirCacheIterator(repo.ReadDirCache());
					}
					if (newTree == null)
					{
						newTree = new FileTreeIterator(repo);
					}
				}
				diffFmt.SetPathFilter(pathFilter);
				if (contextLines >= 0)
				{
					diffFmt.SetContext(contextLines);
				}
				if (destinationPrefix != null)
				{
					diffFmt.SetNewPrefix(destinationPrefix);
				}
				if (sourcePrefix != null)
				{
					diffFmt.SetOldPrefix(sourcePrefix);
				}
				IList<DiffEntry> result = diffFmt.Scan(oldTree, newTree);
				if (showNameAndStatusOnly)
				{
					return result;
				}
				else
				{
					diffFmt.Format(result);
					diffFmt.Flush();
					return result;
				}
			}
			finally
			{
				diffFmt.Release();
			}
		}

		/// <param name="cached">whether to view the changes you staged for the next commit</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetCached(bool cached)
		{
			this.cached = cached;
			return this;
		}

		/// <param name="pathFilter">parameter, used to limit the diff to the named path</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetPathFilter(TreeFilter pathFilter)
		{
			this.pathFilter = pathFilter;
			return this;
		}

		/// <param name="oldTree">the previous state</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetOldTree(AbstractTreeIterator oldTree)
		{
			this.oldTree = oldTree;
			return this;
		}

		/// <param name="newTree">the updated state</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetNewTree(AbstractTreeIterator newTree)
		{
			this.newTree = newTree;
			return this;
		}

		/// <param name="showNameAndStatusOnly">whether to return only names and status of changed files
		/// 	</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetShowNameAndStatusOnly(bool showNameAndStatusOnly
			)
		{
			this.showNameAndStatusOnly = showNameAndStatusOnly;
			return this;
		}

		/// <param name="out">the stream to write line data</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetOutputStream(OutputStream @out)
		{
			this.@out = @out;
			return this;
		}

		/// <summary>Set number of context lines instead of the usual three.</summary>
		/// <remarks>Set number of context lines instead of the usual three.</remarks>
		/// <param name="contextLines">the number of context lines</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetContextLines(int contextLines)
		{
			this.contextLines = contextLines;
			return this;
		}

		/// <summary>Set the given source prefix instead of "a/".</summary>
		/// <remarks>Set the given source prefix instead of "a/".</remarks>
		/// <param name="sourcePrefix">the prefix</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetSourcePrefix(string sourcePrefix)
		{
			this.sourcePrefix = sourcePrefix;
			return this;
		}

		/// <summary>Set the given destination prefix instead of "b/".</summary>
		/// <remarks>Set the given destination prefix instead of "b/".</remarks>
		/// <param name="destinationPrefix">the prefix</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetDestinationPrefix(string destinationPrefix
			)
		{
			this.destinationPrefix = destinationPrefix;
			return this;
		}

		/// <summary>The progress monitor associated with the diff operation.</summary>
		/// <remarks>
		/// The progress monitor associated with the diff operation. By default, this
		/// is set to <code>NullProgressMonitor</code>
		/// </remarks>
		/// <seealso cref="NGit.NullProgressMonitor">NGit.NullProgressMonitor</seealso>
		/// <param name="monitor">a progress monitor</param>
		/// <returns>this instance</returns>
		public virtual NGit.Api.DiffCommand SetProgressMonitor(ProgressMonitor monitor)
		{
			this.monitor = monitor;
			return this;
		}
	}
}
