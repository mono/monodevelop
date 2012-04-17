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
using NGit.Dircache;
using NGit.Internal;
using Sharpen;

namespace NGit.Dircache
{
	/// <summary>
	/// Updates a
	/// <see cref="DirCache">DirCache</see>
	/// by supplying discrete edit commands.
	/// <p>
	/// An editor updates a DirCache by taking a list of
	/// <see cref="PathEdit">PathEdit</see>
	/// commands
	/// and executing them against the entries of the destination cache to produce a
	/// new cache. This edit style allows applications to insert a few commands and
	/// then have the editor compute the proper entry indexes necessary to perform an
	/// efficient in-order update of the index records. This can be easier to use
	/// than
	/// <see cref="DirCacheBuilder">DirCacheBuilder</see>
	/// .
	/// <p>
	/// </summary>
	/// <seealso cref="DirCacheBuilder">DirCacheBuilder</seealso>
	public class DirCacheEditor : BaseDirCacheEditor
	{
		private sealed class _IComparer_71 : IComparer<DirCacheEditor.PathEdit>
		{
			public _IComparer_71()
			{
			}

			public int Compare(DirCacheEditor.PathEdit o1, DirCacheEditor.PathEdit o2)
			{
				byte[] a = o1.path;
				byte[] b = o2.path;
				return DirCache.Cmp(a, a.Length, b, b.Length);
			}
		}

		private static readonly IComparer<DirCacheEditor.PathEdit> EDIT_CMP = new _IComparer_71
			();

		private readonly IList<DirCacheEditor.PathEdit> edits;

		/// <summary>Construct a new editor.</summary>
		/// <remarks>Construct a new editor.</remarks>
		/// <param name="dc">the cache this editor will eventually update.</param>
		/// <param name="ecnt">
		/// estimated number of entries the editor will have upon
		/// completion. This sizes the initial entry table.
		/// </param>
		protected internal DirCacheEditor(DirCache dc, int ecnt) : base(dc, ecnt)
		{
			edits = new AList<DirCacheEditor.PathEdit>();
		}

		/// <summary>Append one edit command to the list of commands to be applied.</summary>
		/// <remarks>
		/// Append one edit command to the list of commands to be applied.
		/// <p>
		/// Edit commands may be added in any order chosen by the application. They
		/// are automatically rearranged by the builder to provide the most efficient
		/// update possible.
		/// </remarks>
		/// <param name="edit">another edit command.</param>
		public virtual void Add(DirCacheEditor.PathEdit edit)
		{
			edits.AddItem(edit);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override bool Commit()
		{
			if (edits.IsEmpty())
			{
				// No changes? Don't rewrite the index.
				//
				cache.Unlock();
				return true;
			}
			return base.Commit();
		}

		public override void Finish()
		{
			if (!edits.IsEmpty())
			{
				ApplyEdits();
				Replace();
			}
		}

		private void ApplyEdits()
		{
			edits.Sort(EDIT_CMP);
			int maxIdx = cache.GetEntryCount();
			int lastIdx = 0;
			foreach (DirCacheEditor.PathEdit e in edits)
			{
				int eIdx = cache.FindEntry(e.path, e.path.Length);
				bool missing = eIdx < 0;
				if (eIdx < 0)
				{
					eIdx = -(eIdx + 1);
				}
				int cnt = Math.Min(eIdx, maxIdx) - lastIdx;
				if (cnt > 0)
				{
					FastKeep(lastIdx, cnt);
				}
				lastIdx = missing ? eIdx : cache.NextEntry(eIdx);
				if (e is DirCacheEditor.DeletePath)
				{
					continue;
				}
				if (e is DirCacheEditor.DeleteTree)
				{
					lastIdx = cache.NextEntry(e.path, e.path.Length, eIdx);
					continue;
				}
				DirCacheEntry ent;
				if (missing)
				{
					ent = new DirCacheEntry(e.path);
					e.Apply(ent);
					if (ent.RawMode == 0)
					{
						throw new ArgumentException(MessageFormat.Format(JGitText.Get().fileModeNotSetForPath
							, ent.PathString));
					}
				}
				else
				{
					ent = cache.GetEntry(eIdx);
					e.Apply(ent);
				}
				FastAdd(ent);
			}
			int cnt_1 = maxIdx - lastIdx;
			if (cnt_1 > 0)
			{
				FastKeep(lastIdx, cnt_1);
			}
		}

		/// <summary>Any index record update.</summary>
		/// <remarks>
		/// Any index record update.
		/// <p>
		/// Applications should subclass and provide their own implementation for the
		/// <see cref="Apply(DirCacheEntry)">Apply(DirCacheEntry)</see>
		/// method. The editor will invoke apply once
		/// for each record in the index which matches the path name. If there are
		/// multiple records (for example in stages 1, 2 and 3), the edit instance
		/// will be called multiple times, once for each stage.
		/// </remarks>
		public abstract class PathEdit
		{
			internal readonly byte[] path;

			/// <summary>Create a new update command by path name.</summary>
			/// <remarks>Create a new update command by path name.</remarks>
			/// <param name="entryPath">path of the file within the repository.</param>
			public PathEdit(string entryPath)
			{
				path = Constants.Encode(entryPath);
			}

			/// <summary>Create a new update command for an existing entry instance.</summary>
			/// <remarks>Create a new update command for an existing entry instance.</remarks>
			/// <param name="ent">
			/// entry instance to match path of. Only the path of this
			/// entry is actually considered during command evaluation.
			/// </param>
			public PathEdit(DirCacheEntry ent)
			{
				path = ent.path;
			}

			/// <summary>Apply the update to a single cache entry matching the path.</summary>
			/// <remarks>
			/// Apply the update to a single cache entry matching the path.
			/// <p>
			/// After apply is invoked the entry is added to the output table, and
			/// will be included in the new index.
			/// </remarks>
			/// <param name="ent">
			/// the entry being processed. All fields are zeroed out if
			/// the path is a new path in the index.
			/// </param>
			public abstract void Apply(DirCacheEntry ent);
		}

		/// <summary>Deletes a single file entry from the index.</summary>
		/// <remarks>
		/// Deletes a single file entry from the index.
		/// <p>
		/// This deletion command removes only a single file at the given location,
		/// but removes multiple stages (if present) for that path. To remove a
		/// complete subtree use
		/// <see cref="DeleteTree">DeleteTree</see>
		/// instead.
		/// </remarks>
		/// <seealso cref="DeleteTree">DeleteTree</seealso>
		public sealed class DeletePath : DirCacheEditor.PathEdit
		{
			/// <summary>Create a new deletion command by path name.</summary>
			/// <remarks>Create a new deletion command by path name.</remarks>
			/// <param name="entryPath">path of the file within the repository.</param>
			public DeletePath(string entryPath) : base(entryPath)
			{
			}

			/// <summary>Create a new deletion command for an existing entry instance.</summary>
			/// <remarks>Create a new deletion command for an existing entry instance.</remarks>
			/// <param name="ent">
			/// entry instance to remove. Only the path of this entry is
			/// actually considered during command evaluation.
			/// </param>
			public DeletePath(DirCacheEntry ent) : base(ent)
			{
			}

			public override void Apply(DirCacheEntry ent)
			{
				throw new NotSupportedException(JGitText.Get().noApplyInDelete);
			}
		}

		/// <summary>Recursively deletes all paths under a subtree.</summary>
		/// <remarks>
		/// Recursively deletes all paths under a subtree.
		/// <p>
		/// This deletion command is more generic than
		/// <see cref="DeletePath">DeletePath</see>
		/// as it can
		/// remove all records which appear recursively under the same subtree.
		/// Multiple stages are removed (if present) for any deleted entry.
		/// <p>
		/// This command will not remove a single file entry. To remove a single file
		/// use
		/// <see cref="DeletePath">DeletePath</see>
		/// .
		/// </remarks>
		/// <seealso cref="DeletePath">DeletePath</seealso>
		public sealed class DeleteTree : DirCacheEditor.PathEdit
		{
			/// <summary>Create a new tree deletion command by path name.</summary>
			/// <remarks>Create a new tree deletion command by path name.</remarks>
			/// <param name="entryPath">
			/// path of the subtree within the repository. If the path
			/// does not end with "/" a "/" is implicitly added to ensure
			/// only the subtree's contents are matched by the command.
			/// The special case "" (not "/"!) deletes all entries.
			/// </param>
			public DeleteTree(string entryPath) : base((entryPath.EndsWith("/") || entryPath.
				Length == 0) ? entryPath : entryPath + "/")
			{
			}

			public override void Apply(DirCacheEntry ent)
			{
				throw new NotSupportedException(JGitText.Get().noApplyInDelete);
			}
		}
	}
}
