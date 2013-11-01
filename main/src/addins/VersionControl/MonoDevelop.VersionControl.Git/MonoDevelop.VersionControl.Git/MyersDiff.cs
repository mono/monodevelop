//
// MyersDiff.cs
//
// Author:
//       Speedy <>
//
// Copyright (c) 2013 Speedy
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using NGit.Api;
using System.Collections.Generic;
using NGit.Diff;
using NGit.Treewalk;
using NGit.Treewalk.Filter;
using Sharpen;
using NGit.Util.IO;
using NGit;
using NGit.Api.Errors;
using NGit.Internal;
using NGit.Dircache;
using System.IO;

namespace MonoDevelop.VersionControl.Git
{
	sealed class MyersDiff : GitCommand<IList<DiffEntry>>
	{
		AbstractTreeIterator oldTree;

		AbstractTreeIterator newTree;

		bool cached;

		TreeFilter pathFilter = TreeFilter.ALL;

		bool showNameAndStatusOnly;

		OutputStream @out;

		int contextLines = -1;

		string sourcePrefix;

		string destinationPrefix;

		NGit.ProgressMonitor monitor = NGit.NullProgressMonitor.INSTANCE;

		protected internal MyersDiff (NGit.Repository repo) : base (repo)
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
		public override IList<DiffEntry> Call()
		{
			DiffFormatter diffFmt;
			if (@out != null && !showNameAndStatusOnly)
			{
				diffFmt = new DiffFormatter(new BufferedOutputStream(@out));
			}
			else
			{
				diffFmt = new DiffFormatter(NullOutputStream.INSTANCE);
			}

			try {
				diffFmt.SetRepository(repo);
			} catch (ArgumentException) {
				// This means diff algorithm is not supported.
			} finally {
				diffFmt.SetDiffAlgorithm (DiffAlgorithm.GetAlgorithm (DiffAlgorithm.SupportedAlgorithm.MYERS));
			}

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
				IList<DiffEntry> result = diffFmt.Scan(oldTree, newTree);
				if (showNameAndStatusOnly)
				{
					return result;
				}
				else
				{
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
					diffFmt.Format(result);
					diffFmt.Flush();
					return result;
				}
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			finally
			{
				diffFmt.Release();
			}
		}

		/// <param name="cached">whether to view the changes you staged for the next commit</param>
		/// <returns>this instance</returns>
		public MyersDiff SetCached(bool cached)
		{
			this.cached = cached;
			return this;
		}

		/// <param name="pathFilter">parameter, used to limit the diff to the named path</param>
		/// <returns>this instance</returns>
		public MyersDiff SetPathFilter(TreeFilter pathFilter)
		{
			this.pathFilter = pathFilter;
			return this;
		}

		/// <param name="oldTree">the previous state</param>
		/// <returns>this instance</returns>
		public MyersDiff SetOldTree(AbstractTreeIterator oldTree)
		{
			this.oldTree = oldTree;
			return this;
		}

		/// <param name="newTree">the updated state</param>
		/// <returns>this instance</returns>
		public MyersDiff SetNewTree(AbstractTreeIterator newTree)
		{
			this.newTree = newTree;
			return this;
		}

		/// <param name="showNameAndStatusOnly">whether to return only names and status of changed files
		/// 	</param>
		/// <returns>this instance</returns>
		public MyersDiff SetShowNameAndStatusOnly(bool showNameAndStatusOnly
		                                                             )
		{
			this.showNameAndStatusOnly = showNameAndStatusOnly;
			return this;
		}

		/// <param name="out">the stream to write line data</param>
		/// <returns>this instance</returns>
		public MyersDiff SetOutputStream(OutputStream @out)
		{
			this.@out = @out;
			return this;
		}

		/// <summary>Set number of context lines instead of the usual three.</summary>
		/// <remarks>Set number of context lines instead of the usual three.</remarks>
		/// <param name="contextLines">the number of context lines</param>
		/// <returns>this instance</returns>
		public MyersDiff SetContextLines(int contextLines)
		{
			this.contextLines = contextLines;
			return this;
		}

		/// <summary>Set the given source prefix instead of "a/".</summary>
		/// <remarks>Set the given source prefix instead of "a/".</remarks>
		/// <param name="sourcePrefix">the prefix</param>
		/// <returns>this instance</returns>
		public MyersDiff SetSourcePrefix(string sourcePrefix)
		{
			this.sourcePrefix = sourcePrefix;
			return this;
		}

		/// <summary>Set the given destination prefix instead of "b/".</summary>
		/// <remarks>Set the given destination prefix instead of "b/".</remarks>
		/// <param name="destinationPrefix">the prefix</param>
		/// <returns>this instance</returns>
		public MyersDiff SetDestinationPrefix(string destinationPrefix
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
		public MyersDiff SetProgressMonitor(ProgressMonitor monitor)
		{
			this.monitor = monitor;
			return this;
		}
	}
}

