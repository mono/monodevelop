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

using System;
using System.Collections.Generic;
using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// This class handles checking out one or two trees merging
	/// with the index (actually a tree too).
	/// </summary>
	/// <remarks>
	/// This class handles checking out one or two trees merging
	/// with the index (actually a tree too).
	/// Three-way merges are no performed. See
	/// <see cref="SetFailOnConflict(bool)">SetFailOnConflict(bool)</see>
	/// .
	/// </remarks>
	[System.ObsoleteAttribute(@"Use org.eclipse.jgit.dircache.DirCacheCheckout.")]
	public class WorkDirCheckout
	{
		internal FilePath root;

		internal GitIndex index;

		private bool failOnConflict = true;

		internal Tree merge;

		/// <summary>
		/// If <code>true</code>, will scan first to see if it's possible to check out,
		/// otherwise throw
		/// <see cref="NGit.Errors.CheckoutConflictException">NGit.Errors.CheckoutConflictException
		/// 	</see>
		/// . If <code>false</code>,
		/// it will silently deal with the problem.
		/// </summary>
		/// <param name="failOnConflict"></param>
		public virtual void SetFailOnConflict(bool failOnConflict)
		{
			this.failOnConflict = failOnConflict;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal WorkDirCheckout(Repository repo, FilePath workDir, GitIndex oldIndex, GitIndex
			 newIndex)
		{
			this.root = workDir;
			this.index = oldIndex;
			this.merge = repo.MapTree(newIndex.WriteTree());
		}

		/// <summary>Create a checkout class for checking out one tree, merging with the index
		/// 	</summary>
		/// <param name="repo"></param>
		/// <param name="root">workdir</param>
		/// <param name="index">current index</param>
		/// <param name="merge">tree to check out</param>
		public WorkDirCheckout(Repository repo, FilePath root, GitIndex index, Tree merge
			)
		{
			this.root = root;
			this.index = index;
			this.merge = merge;
		}

		/// <summary>Create a checkout class for merging and checking our two trees and the index.
		/// 	</summary>
		/// <remarks>Create a checkout class for merging and checking our two trees and the index.
		/// 	</remarks>
		/// <param name="repo"></param>
		/// <param name="root">workdir</param>
		/// <param name="head"></param>
		/// <param name="index"></param>
		/// <param name="merge"></param>
		public WorkDirCheckout(Repository repo, FilePath root, Tree head, GitIndex index, 
			Tree merge) : this(repo, root, index, merge)
		{
			this.head = head;
		}

		/// <summary>Execute this checkout</summary>
		/// <exception cref="System.IO.IOException">System.IO.IOException</exception>
		public virtual void Checkout()
		{
			if (head == null)
			{
				PrescanOneTree();
			}
			else
			{
				PrescanTwoTrees();
			}
			if (!conflicts.IsEmpty())
			{
				if (failOnConflict)
				{
					string[] entries = Sharpen.Collections.ToArray(conflicts, new string[0]);
					throw new NGit.Errors.CheckoutConflictException(entries);
				}
			}
			CleanUpConflicts();
			if (head == null)
			{
				CheckoutOutIndexNoHead();
			}
			else
			{
				CheckoutTwoTrees();
			}
		}

		/// <exception cref="System.IO.FileNotFoundException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		private void CheckoutTwoTrees()
		{
			foreach (string path in removed)
			{
				index.Remove(root, new FilePath(root, path));
			}
			foreach (KeyValuePair<string, ObjectId> entry in updated.EntrySet())
			{
				GitIndex.Entry newEntry = index.AddEntry(merge.FindBlobMember(entry.Key));
				index.CheckoutEntry(root, newEntry);
			}
		}

		internal AList<string> conflicts = new AList<string>();

		internal AList<string> removed = new AList<string>();

		internal Tree head = null;

		internal Dictionary<string, ObjectId> updated = new Dictionary<string, ObjectId>(
			);

		/// <exception cref="System.IO.IOException"></exception>
		private void CheckoutOutIndexNoHead()
		{
			new IndexTreeWalker(index, merge, root, new _AbstractIndexTreeVisitor_166(this)).
				Walk();
		}

		private sealed class _AbstractIndexTreeVisitor_166 : AbstractIndexTreeVisitor
		{
			public _AbstractIndexTreeVisitor_166(WorkDirCheckout _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void VisitEntry(TreeEntry m, GitIndex.Entry i, FilePath f)
			{
				// TODO remove this once we support submodules
				if (f.GetName().Equals(".gitmodules"))
				{
					throw new NotSupportedException(JGitText.Get().submodulesNotSupported);
				}
				if (m == null)
				{
					this._enclosing.index.Remove(this._enclosing.root, f);
					return;
				}
				bool needsCheckout = false;
				if (i == null)
				{
					needsCheckout = true;
				}
				else
				{
					if (i.GetObjectId().Equals(m.GetId()))
					{
						if (i.IsModified(this._enclosing.root, true))
						{
							needsCheckout = true;
						}
					}
					else
					{
						needsCheckout = true;
					}
				}
				if (needsCheckout)
				{
					GitIndex.Entry newEntry = this._enclosing.index.AddEntry(m);
					this._enclosing.index.CheckoutEntry(this._enclosing.root, newEntry);
				}
			}

			private readonly WorkDirCheckout _enclosing;
		}

		/// <exception cref="NGit.Errors.CheckoutConflictException"></exception>
		private void CleanUpConflicts()
		{
			foreach (string c in conflicts)
			{
				FilePath conflict = new FilePath(root, c);
				if (!conflict.Delete())
				{
					throw new NGit.Errors.CheckoutConflictException(MessageFormat.Format(JGitText.Get
						().cannotDeleteFile, c));
				}
				RemoveEmptyParents(conflict);
			}
			foreach (string r in removed)
			{
				FilePath file = new FilePath(root, r);
				file.Delete();
				RemoveEmptyParents(file);
			}
		}

		private void RemoveEmptyParents(FilePath f)
		{
			FilePath parentFile = f.GetParentFile();
			while (!parentFile.Equals(root))
			{
				if (parentFile.List().Length == 0)
				{
					parentFile.Delete();
				}
				else
				{
					break;
				}
				parentFile = parentFile.GetParentFile();
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void PrescanOneTree()
		{
			new IndexTreeWalker(index, merge, root, new _AbstractIndexTreeVisitor_219(this)).
				Walk();
			conflicts.RemoveAll(removed);
		}

		private sealed class _AbstractIndexTreeVisitor_219 : AbstractIndexTreeVisitor
		{
			public _AbstractIndexTreeVisitor_219(WorkDirCheckout _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void VisitEntry(TreeEntry m, GitIndex.Entry i, FilePath file)
			{
				if (m != null)
				{
					if (!file.IsFile())
					{
						this._enclosing.CheckConflictsWithFile(file);
					}
				}
				else
				{
					if (file.Exists())
					{
						this._enclosing.removed.AddItem(i.GetName());
						this._enclosing.conflicts.Remove(i.GetName());
					}
				}
			}

			private readonly WorkDirCheckout _enclosing;
		}

		private AList<string> ListFiles(FilePath file)
		{
			AList<string> list = new AList<string>();
			ListFiles(file, list);
			return list;
		}

		private void ListFiles(FilePath dir, AList<string> list)
		{
			foreach (FilePath f in dir.ListFiles())
			{
				if (f.IsDirectory())
				{
					ListFiles(f, list);
				}
				else
				{
					list.AddItem(Repository.StripWorkDir(root, f));
				}
			}
		}

		/// <returns>a list of conflicts created by this checkout</returns>
		public virtual IList<string> GetConflicts()
		{
			return conflicts;
		}

		/// <returns>a list of all files removed by this checkout</returns>
		public virtual IList<string> GetRemoved()
		{
			return removed;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void PrescanTwoTrees()
		{
			new IndexTreeWalker(index, head, merge, root, new _AbstractIndexTreeVisitor_267(this
				)).Walk();
			// if there's a conflict, don't list it under
			// to-be-removed, since that messed up our next
			// section
			removed.RemoveAll(conflicts);
			foreach (string path in updated.Keys)
			{
				if (index.GetEntry(path) == null)
				{
					FilePath file = new FilePath(root, path);
					if (file.IsFile())
					{
						conflicts.AddItem(path);
					}
					else
					{
						if (file.IsDirectory())
						{
							CheckConflictsWithFile(file);
						}
					}
				}
			}
			conflicts.RemoveAll(removed);
		}

		private sealed class _AbstractIndexTreeVisitor_267 : AbstractIndexTreeVisitor
		{
			public _AbstractIndexTreeVisitor_267(WorkDirCheckout _enclosing)
			{
				this._enclosing = _enclosing;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void VisitEntry(TreeEntry treeEntry, TreeEntry auxEntry, GitIndex.Entry
				 indexEntry, FilePath file)
			{
				if (treeEntry is Tree || auxEntry is Tree)
				{
					throw new ArgumentException(JGitText.Get().cantPassMeATree);
				}
				this._enclosing.ProcessEntry(treeEntry, auxEntry, indexEntry);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void FinishVisitTree(Tree tree, Tree auxTree, string curDir)
			{
				if (curDir.Length == 0)
				{
					return;
				}
				if (auxTree != null)
				{
					if (this._enclosing.index.GetEntry(curDir) != null)
					{
						this._enclosing.removed.AddItem(curDir);
					}
				}
			}

			private readonly WorkDirCheckout _enclosing;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void ProcessEntry(TreeEntry h, TreeEntry m, GitIndex.Entry i)
		{
			ObjectId iId = (i == null ? null : i.GetObjectId());
			ObjectId mId = (m == null ? null : m.GetId());
			ObjectId hId = (h == null ? null : h.GetId());
			string name = (i != null ? i.GetName() : (h != null ? h.GetFullName() : m.GetFullName
				()));
			if (i == null)
			{
				if (h == null)
				{
					updated.Put(name, mId);
				}
				else
				{
					if (m == null)
					{
						removed.AddItem(name);
					}
					else
					{
						updated.Put(name, mId);
					}
				}
			}
			else
			{
				if (h == null)
				{
					if (m == null || mId.Equals(iId))
					{
						if (HasParentBlob(merge, name))
						{
							if (i.IsModified(root, true))
							{
								conflicts.AddItem(name);
							}
							else
							{
								removed.AddItem(name);
							}
						}
					}
					else
					{
						conflicts.AddItem(name);
					}
				}
				else
				{
					if (m == null)
					{
						if (hId.Equals(iId))
						{
							if (i.IsModified(root, true))
							{
								conflicts.AddItem(name);
							}
							else
							{
								removed.AddItem(name);
							}
						}
						else
						{
							conflicts.AddItem(name);
						}
					}
					else
					{
						if (!hId.Equals(mId) && !hId.Equals(iId) && !mId.Equals(iId))
						{
							conflicts.AddItem(name);
						}
						else
						{
							if (hId.Equals(iId) && !mId.Equals(iId))
							{
								if (i.IsModified(root, true))
								{
									conflicts.AddItem(name);
								}
								else
								{
									updated.Put(name, mId);
								}
							}
						}
					}
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private bool HasParentBlob(Tree t, string name)
		{
			if (name.IndexOf("/") == -1)
			{
				return false;
			}
			string parent = Sharpen.Runtime.Substring(name, 0, name.LastIndexOf("/"));
			if (t.FindBlobMember(parent) != null)
			{
				return true;
			}
			return HasParentBlob(t, parent);
		}

		private void CheckConflictsWithFile(FilePath file)
		{
			if (file.IsDirectory())
			{
				AList<string> childFiles = ListFiles(file);
				Sharpen.Collections.AddAll(conflicts, childFiles);
			}
			else
			{
				FilePath parent = file.GetParentFile();
				while (!parent.Equals(root))
				{
					if (parent.IsDirectory())
					{
						break;
					}
					if (parent.IsFile())
					{
						conflicts.AddItem(Repository.StripWorkDir(root, parent));
						break;
					}
					parent = parent.GetParentFile();
				}
			}
		}
	}
}
