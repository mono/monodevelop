/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * Copyright (C) 2009, Nulltoken <emeric.fermas@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */


using System;
using GitSharp.Commands;
using GitSharp.Core;
using CoreRef = GitSharp.Core.Ref;
using CoreCommit = GitSharp.Core.Commit;
using CoreTree = GitSharp.Core.Tree;
using CoreRepository = GitSharp.Core.Repository;

namespace GitSharp
{
	/// <summary>
	/// Represents a branch in git. You can create and manipulate git branches and you can manipulate your working directory using Branch.
	/// 
	/// Note, that new Branch( ...) does not create a new branch in the repository but rather constructs the object to manipulate an existing branch.
	/// To create a new branch use the static Branch.Create API.
	/// </summary>
	public class Branch : Ref
	{

		/// <summary>
		/// Open a branch by resolving a reference (such as HEAD)
		/// </summary>
		/// <param name="ref"></param>
		public Branch(Ref @ref)
			: base(@ref._repo, @ref.Name)
		{
		}

		/// <summary>
		/// Open a branch by branch name (i.e. "master" or "origin/master")
		/// </summary>
		/// <param name="repo"></param>
		/// <param name="name"></param>
		public Branch(Repository repo, string name)
			: base(repo, name)
		{
		}

		internal Branch(Repository repo, CoreRef @ref)
			: this(repo, @ref.Name)
		{
		}

		/// <summary>
		/// Get the branch's full path name relative to the .git directory
		/// </summary>
		public string Fullname
		{
			get { return Constants.R_HEADS + Name; }
		}

		/// <summary>
		/// Returns the latest commit on this branch, or in other words, the commit this branch is pointing to.
		/// </summary>
		public Commit CurrentCommit
		{
			get { return Target as Commit; }
		}

		/// <summary>
		/// True if the branch is the current branch of the repository
		/// </summary>
		public bool IsCurrent
		{
			get { return _repo.CurrentBranch == this; }
		}

		/// <summary>
		/// True if this Ref points to a remote branch.
		/// </summary>
		public bool IsRemote
		{
			get;
			internal set;
		}

		#region --> Merging


		/// <summary>
		/// Merge the given branch into this Branch using the given merge strategy. 
		/// </summary>
		/// <param name="other"></param>
		/// <param name="strategy"></param>
		public MergeResult Merge(Branch other, MergeStrategy strategy)
		{
			return MergeCommand.Execute(new MergeOptions { Branches = new[] { this, other }, MergeStrategy = strategy });
		}


		#endregion

		/// <summary>
		/// Delete this branch
		/// 
		/// Not yet implemented!
		/// </summary>
		public void Delete()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Check out this branch into the working directory and have HEAD point to it.
		/// </summary>
		public void Checkout()
		{
			var db = _repo._internal_repo;
			RefUpdate u = db.UpdateRef(Constants.HEAD);
			u.link(Constants.R_HEADS + this.Name);
			Reset(ResetBehavior.Hard);
		}

		/// <summary>
		/// Rename the Branch. 
		/// 
		/// Not yet implemented!
		/// </summary>
		public void Rename(string name)
		{
			throw new NotImplementedException();
		}

		#region --> Reset


		/// <summary>
		/// Reset this Branch to the current Commit using the given ResetBehavior. <see cref="Reset(GitSharp.Commit,GitSharp.ResetBehavior)"/> for explanation of the reset behavior.
		/// </summary>
		public void Reset(ResetBehavior resetBehavior)
		{
			if (this.CurrentCommit == null)
				throw new InvalidOperationException(string.Format("Branch '{0}' has no commit.", Name));
			var commit = this.CurrentCommit;
			Reset(commit, resetBehavior);
		}

		/// <summary>
		/// Reset this Branch to the named Commit using the given ResetBehavior. <see cref="Reset(GitSharp.Commit,GitSharp.ResetBehavior)"/> for explanation of the reset behavior.
		/// </summary>
		public void Reset(string commitHash, ResetBehavior resetBehavior)
		{
			var commit = new Commit(_repo, commitHash);
			if (!commit.IsCommit)
				throw new ArgumentException(string.Format("The provided hash ({0}) does not point to a commit.", commitHash));
			Reset(commit, resetBehavior);
		}

		/// <summary>
		/// Reset this Branch to the given Commit using the given ResetBehavior.
		/// <para/>
		/// Reset behavior:
		/// <u>
		/// <il>Mixed - Resets the index but not the working tree (i.e., the changed files are preserved but not marked for commit) and reports what has not been updated. This is the default action.</il>
		/// <il>Soft - Does not touch the index file nor the working tree at all, but requires them to be in a good order. This leaves all your changed files "Changes to be committed", as git status would put it.</il>
		/// <il>Hard - Matches the working tree and index to that of the tree being switched to. Any changes to tracked files in the working tree since the commit are lost.</il>
		/// <il>Merge - (NOT IMPLEMENTED) Resets the index to match the tree recorded by the named commit, and updates the files that are different between the named commit and the current commit in the working tree.</il>
		/// </u>
		/// </summary>
		public void Reset(Commit commit, ResetBehavior resetBehavior)
		{
			if (commit == null)
				throw new ArgumentNullException("commit");
			switch (resetBehavior)
			{
				case ResetBehavior.Hard:
					ResetHard(commit);
					break;
				case ResetBehavior.Soft:
					ResetSoft(commit);
					break;
				case ResetBehavior.Mixed:
					ResetMixed(commit);
					break;
				case ResetBehavior.Merge:
					throw new NotImplementedException();
				default:
					throw new NotSupportedException(string.Format("{0} is not supported.", resetBehavior));
			}
		}

		private void ResetMixed(Commit commit)
		{
			if (commit.Tree == null || commit.Tree.InternalTree == null)
				throw new InvalidOperationException("The given commit '" + commit.Hash + "'contains no valid tree.");
			var index = _repo.Index.GitIndex;
			index.ReadTree(commit.Tree.InternalTree);
			index.write();
			Ref.Update("HEAD", commit);
		}

		private static void ResetSoft(Commit commit)
		{
			Ref.Update("HEAD", commit);
		}

		private void ResetHard(Commit commit)
		{
			commit.Checkout();
			_repo._internal_repo.Index.write();
			Ref.Update("HEAD", commit);
		}


		#endregion

		#region --> Branch Creation API

		/// <summary>
		/// Create a new branch based on HEAD.
		/// </summary>
		/// <param name="repo"></param>
		/// <param name="name">The name of the branch to create (i.e. "master", not "refs/heads/master")</param>
		/// <returns>returns the newly created Branch object</returns>
		public static Branch Create(Repository repo, string name)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("Branch name must not be null or empty");
			Ref.Update("refs/heads/" + name, repo.Head.CurrentCommit);
			return new Branch(repo, name);
		}

		/// <summary>
		/// Create a new branch basing on the given commit
		/// </summary>
		/// <param name="repo"></param>
		/// <param name="name">The name of the branch to create (i.e. "master", not "refs/heads/master")</param>
		/// <param name="commit">The commit to base the branch on.</param>
		/// <returns>returns the newly created Branch object</returns>
		public static Branch Create(Repository repo, string name, Commit commit)
		{
			if (string.IsNullOrEmpty(name))
				throw new ArgumentException("Branch name must not be null or empty", "name");
			if (commit == null || !commit.IsCommit)
				throw new ArgumentException("Invalid commit", "commit");
			Ref.Update("refs/heads/" + name, commit);
			return new Branch(repo, name);
		}


		#endregion


		public override string ToString()
		{
			return "Branch[" + Name + "]";
		}

	}
}
