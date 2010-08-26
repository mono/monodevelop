/*
 * Copyright (C) 2009-2010, Henon <meinrad.recheis@gmail.com>
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
using GitSharp.Core;
using ObjectId = GitSharp.Core.ObjectId;
using CoreRef = GitSharp.Core.Ref;
using CoreCommit = GitSharp.Core.Commit;
using CoreTree = GitSharp.Core.Tree;
using FileTreeEntry = GitSharp.Core.FileTreeEntry;

namespace GitSharp
{

	/// <summary>
	/// Represents a directory in the git repository.
	/// </summary>
	public class Tree : AbstractTreeNode
	{
		internal Tree(Repository repo, ObjectId id) : base(repo, id) { }

		internal Tree(Repository repo, CoreTree tree)
			: base(repo, tree.Id)
		{
			_internal_tree = tree;
		}

		private CoreTree _internal_tree;

		internal CoreTree InternalTree
		{
			get
			{
				if (_internal_tree == null)
					try
					{
						_internal_tree = _repo._internal_repo.MapTree(_id);
					}
					catch (Exception)
					{
						// the commit object is invalid. however, we can not allow exceptions here because they would not be expected.
					}
				return _internal_tree;
			}
		}

		public override string Name
		{
			get
			{
				if (InternalTree == null)
					return null;
				if (InternalTree.IsRoot)
					return "";
				return InternalTree.Name;
			}
		}

		/// <summary>
		/// True if the tree has no parent.
		/// </summary>
		public bool IsRoot
		{
			get
			{
				if (InternalTree == null)
					return true;
				return InternalTree.IsRoot;
			}
		}

		public override Tree Parent
		{
			get
			{
				if (InternalTree == null)
					return null;
				if (InternalTree.Parent == null)
					return null;
				return new Tree(_repo, InternalTree.Parent);
			}
		}

		/// <summary>
		/// Entries of the tree. These are either Tree or Leaf objects representing sub-directories or files.
		/// </summary>
		public IEnumerable<AbstractObject> Children
		{
			get
			{
				if (InternalTree == null)
					return new Leaf[0];

				// no GitLink support in JGit, so just skip them here to not cause problems
				return InternalTree.Members.Where(te => !(te is GitLinkTreeEntry)).Select(
					 tree_entry =>
					 {
						 if (tree_entry is FileTreeEntry)
							 return new Leaf(_repo, tree_entry as FileTreeEntry) as AbstractObject;

						 return new Tree(_repo, tree_entry as CoreTree) as AbstractObject;
					 }).ToArray();
			}
		}

		/// <summary>
		/// Tree entries representing this directory's subdirectories
		/// </summary>
		public IEnumerable<Tree> Trees
		{
			get
			{
				return Children.Where(child => child.IsTree).Cast<Tree>().ToArray();
			}
		}

		/// <summary>
		/// Leaf entries representing this directory's files
		/// </summary>
		public IEnumerable<Leaf> Leaves
		{
			get
			{
				return Children.Where(child => child.IsBlob).Cast<Leaf>().ToArray();
			}
		}

		public override string Path
		{
			get
			{
				if (InternalTree == null)
					return null;
				if (InternalTree.IsRoot)
					return "";
				return InternalTree.FullName;
			}
		}

		public override int Permissions
		{

			get
			{
				if (InternalTree == null)
					return 0;
				return InternalTree.Mode.Bits;
			}
		}

		public override string ToString()
		{
			return "Tree[" + ShortHash + "]";
		}

		/// <summary>
		/// Find a Blob or Tree by traversing the tree along the given path. You can access not only direct children
		/// of the tree but any descendant of this tree.
		/// <para/>
		/// The path's directory seperators may be both forward or backslash, it is converted automatically to the internal representation.
		/// <para/>
		/// Throws IOException.
		/// </summary>
		/// <param name="path">Relative path to a file or directory in the git tree. For directories a trailing slash is allowed</param>
		/// <returns>A tree or blob object representing the referenced object</returns>
		public AbstractObject this[string path]
		{
			get
			{
				if (path == "")
					return this;
				var tree_entry = _internal_tree.FindBlobMember(path);
				if (tree_entry == null)
					tree_entry = _internal_tree.findTreeMember(path);
				if (tree_entry == null)
					return null;
				if (tree_entry.IsTree)
					return new Tree(_repo, tree_entry as CoreTree);
				else if (tree_entry.IsBlob)
					return new Leaf(_repo, tree_entry as FileTreeEntry);
				else // if (tree_entry.IsCommit || tree_entry.IsTag)
					return AbstractObject.Wrap(_repo, tree_entry.Id);
			}
		}

		public static implicit operator CoreTree(Tree t)
		{
			return t._internal_tree;
		}


	}
}
