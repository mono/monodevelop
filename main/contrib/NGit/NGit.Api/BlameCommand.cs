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
using NGit.Blame;
using NGit.Diff;
using NGit.Dircache;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// Blame command for building a
	/// <see cref="NGit.Blame.BlameResult">NGit.Blame.BlameResult</see>
	/// for a file path.
	/// </summary>
	public class BlameCommand : GitCommand<BlameResult>
	{
		private string path;

		private DiffAlgorithm diffAlgorithm;

		private RawTextComparator textComparator;

		private ObjectId startCommit;

		private ICollection<ObjectId> reverseEndCommits;

		private bool followFileRenames;

		/// <param name="repo"></param>
		protected internal BlameCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>Set file path</summary>
		/// <param name="filePath"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.BlameCommand SetFilePath(string filePath)
		{
			this.path = filePath;
			return this;
		}

		/// <summary>Set diff algorithm</summary>
		/// <param name="diffAlgorithm"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.BlameCommand SetDiffAlgorithm(DiffAlgorithm diffAlgorithm
			)
		{
			this.diffAlgorithm = diffAlgorithm;
			return this;
		}

		/// <summary>Set raw text comparator</summary>
		/// <param name="textComparator"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.BlameCommand SetTextComparator(RawTextComparator textComparator
			)
		{
			this.textComparator = textComparator;
			return this;
		}

		/// <summary>Set start commit id</summary>
		/// <param name="commit"></param>
		/// <returns>this command</returns>
		public virtual NGit.Api.BlameCommand SetStartCommit(AnyObjectId commit)
		{
			this.startCommit = commit.ToObjectId();
			return this;
		}

		/// <summary>Enable (or disable) following file renames.</summary>
		/// <remarks>
		/// Enable (or disable) following file renames.
		/// <p>
		/// If true renames are followed using the standard FollowFilter behavior
		/// used by RevWalk (which matches
		/// <code>git log --follow</code>
		/// in the C
		/// implementation). This is not the same as copy/move detection as
		/// implemented by the C implementation's of
		/// <code>git blame -M -C</code>
		/// .
		/// </remarks>
		/// <param name="follow">enable following.</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.BlameCommand SetFollowFileRenames(bool follow)
		{
			followFileRenames = Sharpen.Extensions.ValueOf(follow);
			return this;
		}

		/// <summary>Configure the command to compute reverse blame (history of deletes).</summary>
		/// <remarks>Configure the command to compute reverse blame (history of deletes).</remarks>
		/// <param name="start">
		/// oldest commit to traverse from. The result file will be loaded
		/// from this commit's tree.
		/// </param>
		/// <param name="end">
		/// most recent commit to stop traversal at. Usually an active
		/// branch tip, tag, or HEAD.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual NGit.Api.BlameCommand Reverse(AnyObjectId start, AnyObjectId end)
		{
			return Reverse(start, Sharpen.Collections.Singleton(end.ToObjectId()));
		}

		/// <summary>Configure the generator to compute reverse blame (history of deletes).</summary>
		/// <remarks>Configure the generator to compute reverse blame (history of deletes).</remarks>
		/// <param name="start">
		/// oldest commit to traverse from. The result file will be loaded
		/// from this commit's tree.
		/// </param>
		/// <param name="end">
		/// most recent commits to stop traversal at. Usually an active
		/// branch tip, tag, or HEAD.
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		/// <exception cref="System.IO.IOException">the repository cannot be read.</exception>
		public virtual NGit.Api.BlameCommand Reverse(AnyObjectId start, ICollection<ObjectId
			> end)
		{
			startCommit = start.ToObjectId();
			reverseEndCommits = new AList<ObjectId>(end);
			return this;
		}

		/// <summary>
		/// Generate a list of lines with information about when the lines were
		/// introduced into the file path.
		/// </summary>
		/// <remarks>
		/// Generate a list of lines with information about when the lines were
		/// introduced into the file path.
		/// </remarks>
		/// <returns>list of lines</returns>
		/// <exception cref="NGit.Api.Errors.JGitInternalException"></exception>
		public override BlameResult Call()
		{
			CheckCallable();
			BlameGenerator gen = new BlameGenerator(repo, path);
			try
			{
				if (diffAlgorithm != null)
				{
					gen.SetDiffAlgorithm(diffAlgorithm);
				}
				if (textComparator != null)
				{
					gen.SetTextComparator(textComparator);
				}
				if (followFileRenames != null)
				{
					gen.SetFollowFileRenames(followFileRenames);
				}
				if (reverseEndCommits != null)
				{
					gen.Reverse(startCommit, reverseEndCommits);
				}
				else
				{
					if (startCommit != null)
					{
						gen.Push(null, startCommit);
					}
					else
					{
						gen.Push(null, repo.Resolve(Constants.HEAD));
						if (!repo.IsBare)
						{
							DirCache dc = repo.ReadDirCache();
							int entry = dc.FindEntry(path);
							if (0 <= entry)
							{
								gen.Push(null, dc.GetEntry(entry).GetObjectId());
							}
							FilePath inTree = new FilePath(repo.WorkTree, path);
							if (inTree.IsFile())
							{
								gen.Push(null, new RawText(inTree));
							}
						}
					}
				}
				return gen.ComputeBlameResult();
			}
			catch (IOException e)
			{
				throw new JGitInternalException(e.Message, e);
			}
			finally
			{
				gen.Release();
			}
		}
	}
}
