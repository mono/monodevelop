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
using NGit.Merge;
using NGit.Revwalk;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Api
{
	/// <summary>
	/// A class used to execute a
	/// <code>cherry-pick</code>
	/// command. It has setters for all
	/// supported options and arguments of this command and a
	/// <see cref="Call()">Call()</see>
	/// method
	/// to finally execute the command. Each instance of this class should only be
	/// used for one invocation of the command (means: one call to
	/// <see cref="Call()">Call()</see>
	/// )
	/// </summary>
	/// <seealso><a
	/// *      href="http://www.kernel.org/pub/software/scm/git/docs/git-cherry-pick.html"
	/// *      >Git documentation about cherry-pick</a></seealso>
	public class CherryPickCommand : GitCommand<RevCommit>
	{
		private IList<Ref> commits = new List<Ref>();

		private IList<Ref> cherryPickedRefs = new List<Ref>();

		/// <param name="repo"></param>
		protected internal CherryPickCommand(Repository repo) : base(repo)
		{
		}

		/// <summary>
		/// Executes the
		/// <code>Cherry-Pick</code>
		/// command with all the options and
		/// parameters collected by the setter methods (e.g.
		/// <see cref="Include(NGit.Ref)">Include(NGit.Ref)</see>
		/// of
		/// this class. Each instance of this class should only be used for one
		/// invocation of the command. Don't call this method twice on an instance.
		/// </summary>
		/// <returns>
		/// on success the
		/// <see cref="NGit.Revwalk.RevCommit">NGit.Revwalk.RevCommit</see>
		/// pointed to by the new HEAD is
		/// returned. If a failure occurred during cherry-pick
		/// <code>null</code> is returned. The list of successfully
		/// cherry-picked
		/// <see cref="NGit.Ref">NGit.Ref</see>
		/// 's can be obtained by calling
		/// <see cref="GetCherryPickedRefs()">GetCherryPickedRefs()</see>
		/// </returns>
		/// <exception cref="NGit.Api.Errors.GitAPIException"></exception>
		public override RevCommit Call()
		{
			RevCommit newHead = null;
			CheckCallable();
			RevWalk revWalk = new RevWalk(repo);
			try
			{
				// get the head commit
				Ref headRef = repo.GetRef(Constants.HEAD);
				if (headRef == null)
				{
					throw new NoHeadException(JGitText.Get().commitOnRepoWithoutHEADCurrentlyNotSupported
						);
				}
				RevCommit headCommit = revWalk.ParseCommit(headRef.GetObjectId());
				newHead = headCommit;
				// loop through all refs to be cherry-picked
				foreach (Ref src in commits)
				{
					// get the commit to be cherry-picked
					// handle annotated tags
					ObjectId srcObjectId = src.GetPeeledObjectId();
					if (srcObjectId == null)
					{
						srcObjectId = src.GetObjectId();
					}
					RevCommit srcCommit = revWalk.ParseCommit(srcObjectId);
					// get the parent of the commit to cherry-pick
					if (srcCommit.ParentCount != 1)
					{
						throw new MultipleParentsNotAllowedException(JGitText.Get().canOnlyCherryPickCommitsWithOneParent
							);
					}
					RevCommit srcParent = srcCommit.GetParent(0);
					revWalk.ParseHeaders(srcParent);
					ResolveMerger merger = (ResolveMerger)((ThreeWayMerger)MergeStrategy.RESOLVE.NewMerger
						(repo));
					merger.SetWorkingTreeIterator(new FileTreeIterator(repo));
					merger.SetBase(srcParent.Tree);
					if (merger.Merge(headCommit, srcCommit))
					{
						if (AnyObjectId.Equals(headCommit.Tree.Id, merger.GetResultTreeId()))
						{
							continue;
						}
						DirCacheCheckout dco = new DirCacheCheckout(repo, headCommit.Tree, repo.LockDirCache
							(), merger.GetResultTreeId());
						dco.SetFailOnConflict(true);
						dco.Checkout();
						newHead = new Git(GetRepository()).Commit().SetMessage(srcCommit.GetFullMessage()
							).SetAuthor(srcCommit.GetAuthorIdent()).Call();
						cherryPickedRefs.AddItem(src);
					}
					else
					{
						return null;
					}
				}
			}
			catch (IOException e)
			{
				throw new JGitInternalException(MessageFormat.Format(JGitText.Get().exceptionCaughtDuringExecutionOfCherryPickCommand
					, e), e);
			}
			finally
			{
				revWalk.Release();
			}
			return newHead;
		}

		/// <param name="commit">
		/// a reference to a commit which is cherry-picked to the current
		/// head
		/// </param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CherryPickCommand Include(Ref commit)
		{
			CheckCallable();
			commits.AddItem(commit);
			return this;
		}

		/// <param name="commit">the Id of a commit which is cherry-picked to the current head
		/// 	</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CherryPickCommand Include(AnyObjectId commit)
		{
			return Include(commit.GetName(), commit);
		}

		/// <param name="name">a name given to the commit</param>
		/// <param name="commit">the Id of a commit which is cherry-picked to the current head
		/// 	</param>
		/// <returns>
		/// 
		/// <code>this</code>
		/// </returns>
		public virtual NGit.Api.CherryPickCommand Include(string name, AnyObjectId commit
			)
		{
			return Include(new ObjectIdRef.Unpeeled(RefStorage.LOOSE, name, commit.Copy()));
		}

		/// <returns>
		/// the list of successfully cherry-picked
		/// <see cref="NGit.Ref">NGit.Ref</see>
		/// 's. Never
		/// <code>null</code> but maybe an empty list if no commit was
		/// successfully cherry-picked
		/// </returns>
		public virtual IList<Ref> GetCherryPickedRefs()
		{
			return cherryPickedRefs;
		}
	}
}
