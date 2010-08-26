/*
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitSharp.Commands;
using GitSharp.Core;
using GitSharp.Core.RevWalk;
using GitSharp.Core.Util;
using GitSharp.Core.Util.JavaHelper;
using ObjectId = GitSharp.Core.ObjectId;
using CoreRef = GitSharp.Core.Ref;
using CoreTree = GitSharp.Core.Tree;
using System.IO;
using GitSharp.Core.TreeWalk;
using GitSharp.Core.TreeWalk.Filter;
using System.Diagnostics;
using System.Collections;

namespace GitSharp
{
	/// <summary>
	/// Represents a revision of the files and directories tracked in the repository.
	/// </summary>
	public class Commit : AbstractObject
	{

		public Commit(Repository repo, string hash_or_name)
			: base(repo, hash_or_name)
		{
		}

		internal Commit(Repository repo, CoreRef @ref)
			: base(repo, @ref.ObjectId)
		{
		}

		internal Commit(Repository repo, Core.Commit internal_commit)
			: base(repo, internal_commit.CommitId)
		{
			_internal_commit = internal_commit;
		}

		internal Commit(Repository repo, ObjectId id)
			: base(repo, id)
		{
		}

		private Core.Commit _internal_commit;

		private Core.Commit InternalCommit
		{
			get
			{
				if (_internal_commit == null)
					try
					{
						_internal_commit = _repo._internal_repo.MapCommit(_id);
					}
					catch (Exception)
					{
						// the commit object is invalid. however, we can not allow exceptions here because they would not be expected.
					}
				return _internal_commit;
			}
		}

		#region --> Commit Properties


		public bool IsValid
		{
			get
			{
				return InternalCommit is Core.Commit;
			}
		}

		/// <summary>
		/// The commit message.
		/// </summary>
		public string Message
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return null;
				return InternalCommit.Message;
			}
		}

		/// <summary>
		/// The encoding of the commit details.
		/// </summary>
		public Encoding Encoding
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return null;
				return InternalCommit.Encoding;
			}
		}

		/// <summary>
		/// The author of the change set represented by this commit. 
		/// </summary>
		public Author Author
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return null;
				return new Author() { Name = InternalCommit.Author.Name, EmailAddress = InternalCommit.Author.EmailAddress };
			}
		}

		/// <summary>
		/// The person who committed the change set by reusing authorship information from another commit. If the commit was created by the author himself, Committer is equal to the Author.
		/// </summary>
		public Author Committer
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return null;
				var committer = InternalCommit.Committer;
				if (committer == null) // this is null if the author committed himself
					return Author;
				return new Author() { Name = committer.Name, EmailAddress = committer.EmailAddress };
			}
		}

		/// <summary>
		/// Original timestamp of the commit created by Author. 
		/// </summary>
		public DateTimeOffset AuthorDate
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return DateTimeOffset.MinValue;
				return InternalCommit.Author.When.MillisToDateTimeOffset(InternalCommit.Author.TimeZoneOffset);
			}
		}

		/// <summary>
		/// Final timestamp of the commit, after Committer has re-committed Author's commit.
		/// </summary>
		public DateTimeOffset CommitDate
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return DateTimeOffset.MinValue;
				var committer = InternalCommit.Committer;
				if (committer == null) // this is null if the author committed himself
					committer = InternalCommit.Author;
				return committer.When.MillisToDateTimeOffset(committer.TimeZoneOffset);
			}
		}

		/// <summary>
		/// Returns true if the commit was created by the author of the change set himself.
		/// </summary>
		public bool IsCommittedByAuthor
		{
			get
			{
				return Author == Committer;
			}
		}

		/// <summary>
		/// Returns all parent commits.
		/// </summary>
		public IEnumerable<Commit> Parents
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return new Commit[0];
				return InternalCommit.ParentIds.Select(parent_id => new Commit(_repo, parent_id)).ToArray();
			}
		}

		/// <summary>
		/// True if the commit has at least one parent.
		/// </summary>
		public bool HasParents
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return false;
				return InternalCommit.ParentIds.Length > 0;
			}
		}

		/// <summary>
		/// The first parent commit if the commit has at least one parent, null otherwise.
		/// </summary>
		public Commit Parent
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return null;
				if (HasParents)
					return new Commit(_repo, InternalCommit.ParentIds[0]);
				return null;
			}
		}

		/// <summary>
		/// The commit's reference to the root of the directory structure of the revision.
		/// </summary>
		public Tree Tree
		{
			get
			{
				if (InternalCommit == null) // this might happen if the object was created with an incorrect reference
					return null;
				try
				{
					return new Tree(_repo, InternalCommit.TreeEntry);
				}
				catch (GitSharp.Core.Exceptions.MissingObjectException)
				{
					return null; // relieve the client of having to catch the exception! If tree is null it is obvious that the tree could not be found.
				}
			}
		}

		/// <summary>
		///  Returns an iterator over all ancestor-commits of this commit. 
		/// </summary>
		public IEnumerable<Commit> Ancestors
		{
			get
			{
				var revwalk = new RevWalk(_repo);
				revwalk.RevSortStrategy.Add(RevSort.Strategy.COMMIT_TIME_DESC);
				revwalk.RevSortStrategy.Add(RevSort.Strategy.TOPO);
				revwalk.markStart(revwalk.parseCommit(_id));
				return
					from revcommit in revwalk.Skip(1) // skip this commit
					select new Commit(_repo, revcommit.AsCommit(revwalk));
			}
		}


		#endregion

		#region --> Checkout


		/// <summary>
		/// Checkout this commit into the working directory. Does not change HEAD.
		/// <para/>
		/// <seealso cref="Branch.Checkout"/> and <seealso cref="Index.Checkout()"/>.
		/// </summary>
		public void Checkout()
		{
			Checkout(_repo.WorkingDirectory);
		}

		private void Checkout(string working_directory) // [henon] made this private to not confuse with Checkout( paths ). It seems to be not a common use case anyway and could be better exposed via the CheckoutCommand
		{
			if (InternalCommit == null)
				throw new InvalidOperationException("Unable to checkout this commit. It was not initialized properly (i.e. the hash is not pointing to a commit object).");
			if (working_directory == null)
				throw new ArgumentException("Path to checkout directory must not be null");
			if (new DirectoryInfo(working_directory).Exists == false)
				throw new IOException("Cannot checkout into non-existent directory: " + working_directory);
			var db = _repo._internal_repo;
			var index = _repo.Index.GitIndex;
			index.RereadIfNecessary();
			CoreTree tree = InternalCommit.TreeEntry;
			var co = new GitSharp.Core.WorkDirCheckout(db, new DirectoryInfo(working_directory), index, tree);
			co.checkout();
		}

		/// <summary>
		/// Check out the given paths into the working directory. Files in the working directory will be overwritten.
		/// <para/>
		/// See also <seealso cref="Index.Checkout(string[])"/> to check out paths from the index.
		/// </summary>
		/// <param name="paths">Relative paths of the files to check out.</param>
		/// Throws a lot of IO and Security related exceptions.
		public void Checkout(params string[] paths)
		{
			var tree = this.Tree;
			if (tree == null || !tree.IsTree)
				throw new InvalidOperationException("This commit doesn't seem to have a valid tree.");
			foreach (string path in paths)
			{
				var leaf = tree[path] as Leaf;
				if (leaf == null)
					throw new ArgumentException("The given path does not exist in this commit: " + path);
				var filename = Path.Combine(_repo.WorkingDirectory, path);
				new FileInfo(filename).Directory.Mkdirs();
				File.WriteAllBytes(filename, leaf.Blob.RawData); // todo: hmm, what is with file permissions and other file attributes?
			}
		}


		#endregion

		#region --> Diffing commits


		/// <summary>
		/// Compare reference commit against compared commit. You may pass in a null commit (i.e. for getting the changes of the first commit)
		/// </summary>
		/// <param name="reference">Usually the more recent commit</param>
		/// <param name="compared">Usually an ancestor of the reference commit</param>
		/// <returns>a list of changes (<see cref="Change"/>)</returns>
		public static IEnumerable<Change> CompareCommits(Commit reference, Commit compared)
		{
			var changes = new List<Change>();
			if (reference == null && compared == null)
				return changes;
			var repo = (reference ?? compared).Repository;
			var ref_tree = (reference != null ? reference.Tree._id : ObjectId.ZeroId);
			var compared_tree = (compared != null ? compared.Tree._id : ObjectId.ZeroId);
			var db = repo._internal_repo;
			var walk = new TreeWalk(db);
			if (reference == null || compared == null)
				walk.reset((reference ?? compared).Tree._id);
			else
				walk.reset(new GitSharp.Core.AnyObjectId[] {ref_tree, compared_tree});
			walk.Recursive = true;
			walk.setFilter(AndTreeFilter.create(TreeFilter.ANY_DIFF, TreeFilter.ALL));

			return CalculateCommitDiff(repo, walk, new[] { reference, compared });
		}

		/// <summary>
		/// compare the given commits and return the changes.
		/// </summary>
		/// <param name="repo"></param>
		/// <param name="walk"></param>
		/// <param name="commits">first commit in the array is the commit that is compared against the others which are his ancestors (i.e. from different branches)</param>
		/// <returns></returns>
		private static IEnumerable<Change> CalculateCommitDiff(Repository repo, TreeWalk walk, Commit[] commits)
		{
			while (walk.next())
			{
				int m0 = walk.getRawMode(0);
				if (walk.getTreeCount() == 2)
				{
					int m1 = walk.getRawMode(1);
					var change = new Change
					{
						ReferenceCommit = commits[0],
						ComparedCommit = commits[1],
						ReferencePermissions = walk.getFileMode(0).Bits,
						ComparedPermissions = walk.getFileMode(1).Bits,
						Name = walk.getNameString(),
						Path = walk.getPathString(),
					};
					if (m0 != 0 && m1 == 0)
					{
						change.ChangeType = ChangeType.Added;
						change.ComparedObject = Wrap(repo, walk.getObjectId(0));
					}
					else if (m0 == 0 && m1 != 0)
					{
						change.ChangeType = ChangeType.Deleted;
						change.ReferenceObject = Wrap(repo, walk.getObjectId(0));
					}
					else if (m0 != m1 && walk.idEqual(0, 1))
					{
						change.ChangeType = ChangeType.TypeChanged;
						change.ReferenceObject = Wrap(repo, walk.getObjectId(0));
						change.ComparedObject = Wrap(repo, walk.getObjectId(1));
					}
					else
					{
						change.ChangeType = ChangeType.Modified;
						change.ReferenceObject = Wrap(repo, walk.getObjectId(0));
						change.ComparedObject = Wrap(repo, walk.getObjectId(1));
					}
					yield return change;
				}
				else
				{
					var raw_modes = new int[walk.getTreeCount()-1];
					for(int i = 0;i<walk.getTreeCount()-1; i++)
						raw_modes[i] = walk.getRawMode(i+1);
					var change = new Change
					{
						ReferenceCommit = commits[0],
						//ComparedCommit = compared,
						Name = walk.getNameString(),
						Path = walk.getPathString(),
					};
					if (m0 != 0 && raw_modes.All(m1 => m1 == 0))
					{
						change.ChangeType = ChangeType.Added;
						change.ComparedObject = Wrap(repo, walk.getObjectId(0));
						yield return change;
					}
					else if (m0 == 0 && raw_modes.Any(m1 => m1 != 0))
					{
						change.ChangeType = ChangeType.Deleted;
						yield return change;
					}
					else if (raw_modes.Select((m1, i) => new { Mode = m1, Index = i + 1 }).All(x => !walk.idEqual(0, x.Index))) // TODO: not sure if this condition suffices in some special cases.
					{
						change.ChangeType = ChangeType.Modified;
						change.ReferenceObject = Wrap(repo, walk.getObjectId(0));
						yield return change;
					}
					else if (raw_modes.Select((m1, i) => new { Mode = m1, Index = i + 1 }).Any(x => m0 != x.Mode && walk.idEqual(0, x.Index)))
					{
						change.ChangeType = ChangeType.TypeChanged;
						change.ReferenceObject = Wrap(repo, walk.getObjectId(0));
						yield return change;
					}
				}
			}
		}

		/// <summary>
		/// Compares this commit against another one and returns all changes between the two.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public IEnumerable<Change> CompareAgainst(Commit other)
		{
			return CompareCommits(this, other);
		}

		/// <summary>
		/// Returns the changes of this commit vs. it's parent commit(s). Works for the first commit too.
		/// </summary>
		public IEnumerable<Change> Changes
		{
			get
			{
				if (this.Parents.Count() == 0)
					return CompareCommits(this, null);
				else
					return CompareAgainstParents();
			}
		}

		/// <summary>
		/// Compare a commit against (one or multiple) parents (merge scenario)
		/// </summary>
		/// <returns>Changes of the commit</returns>
		private IEnumerable<Change> CompareAgainstParents()
		{
			var db = _repo._internal_repo;
			var tree_ids = new[] { this }.Concat(Parents).Select(c => c.Tree._id).ToArray();
			var walk = new TreeWalk(db);
			walk.reset(tree_ids);
			walk.Recursive = true;
			walk.setFilter(AndTreeFilter.create(TreeFilter.ANY_DIFF, TreeFilter.ALL));

			return CalculateCommitDiff(_repo, walk, new[] { this }.Concat(Parents).ToArray());
		}


		#endregion

		#region --> Committing


		public static Commit Create(string message, Commit parent, Tree tree)
		{
			if (tree == null)
				throw new ArgumentException("tree must not be null");
			var repo = tree.Repository;
			var author = Author.GetDefaultAuthor(parent._repo);
			return Create(message, parent, tree, author, author, DateTimeOffset.Now);
		}

		public static Commit Create(string message, Commit parent, Tree tree, Author author)
		{
			return Create(message, parent, tree, author, author, DateTimeOffset.Now);
		}

		public static Commit Create(string message, Commit parent, Tree tree, Author author, Author committer, DateTimeOffset time)
		{
			return Create(message, (parent == null ? new Commit[0] : new[] { parent }), tree, author, committer, time);
		}

		public static Commit Create(string message, IEnumerable<Commit> parents, Tree tree, Author author, Author committer, DateTimeOffset time)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("message must not be null or empty");
			if (tree == null)
				throw new ArgumentException("tree must not be null");
			var repo = tree.Repository;
			var corecommit = new Core.Commit(repo._internal_repo);
			if (parents != null)
				corecommit.ParentIds = parents.Select(parent => parent._id).ToArray();
			corecommit.Author = new Core.PersonIdent(author.Name, author.EmailAddress, time.ToMillisecondsSinceEpoch(), (int)time.Offset.TotalMinutes);
			corecommit.Committer = new Core.PersonIdent(committer.Name, committer.EmailAddress, time.ToMillisecondsSinceEpoch(), (int)time.Offset.TotalMinutes);
			corecommit.Message = message;
			corecommit.TreeEntry = tree.InternalTree;
			corecommit.Encoding = GetCommitEncoding(repo);
			corecommit.Save();
			return new Commit(repo, corecommit);
		}

		private static Encoding GetCommitEncoding(Repository repository)
		{
			string encodingAlias = repository.Config["i18n.commitencoding"];
			if (encodingAlias == null)
			{
				// No commitencoding has been specified in the config
				return null;
			}
			return Charset.forName(encodingAlias);
		}


		#endregion

		public static implicit operator Core.Commit(Commit c)
		{
			return c.InternalCommit;
		}

		public override string ToString()
		{
			return "Commit[" + ShortHash + "]";
		}
	}
}
