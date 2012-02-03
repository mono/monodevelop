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
using System.Text;
using NGit;
using NGit.Diff;
using NGit.Treewalk;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>A value class representing a change to a file</summary>
	public class DiffEntry
	{
		/// <summary>Magical SHA1 used for file adds or deletes</summary>
		internal static readonly AbbreviatedObjectId A_ZERO = AbbreviatedObjectId.FromObjectId
			(ObjectId.ZeroId);

		/// <summary>Magical file name used for file adds or deletes.</summary>
		/// <remarks>Magical file name used for file adds or deletes.</remarks>
		public static readonly string DEV_NULL = "/dev/null";

		/// <summary>General type of change a single file-level patch describes.</summary>
		/// <remarks>General type of change a single file-level patch describes.</remarks>
		public enum ChangeType
		{
			ADD,
			MODIFY,
			DELETE,
			RENAME,
			COPY
		}

		/// <summary>Specify the old or new side for more generalized access.</summary>
		/// <remarks>Specify the old or new side for more generalized access.</remarks>
		public enum Side
		{
			OLD,
			NEW
		}

		/// <summary>Create an empty DiffEntry</summary>
		public DiffEntry()
		{
		}

		// reduce the visibility of the default constructor
		/// <summary>Convert the TreeWalk into DiffEntry headers.</summary>
		/// <remarks>Convert the TreeWalk into DiffEntry headers.</remarks>
		/// <param name="walk">the TreeWalk to walk through. Must have exactly two trees.</param>
		/// <returns>headers describing the changed files.</returns>
		/// <exception cref="System.IO.IOException">the repository cannot be accessed.</exception>
		/// <exception cref="System.ArgumentException">When given TreeWalk doesn't have exactly two trees.
		/// 	</exception>
		public static IList<NGit.Diff.DiffEntry> Scan(TreeWalk walk)
		{
			return Scan(walk, false);
		}

		/// <summary>
		/// Convert the TreeWalk into DiffEntry headers, depending on
		/// <code>includeTrees</code>
		/// it will add tree objects into result or not.
		/// </summary>
		/// <param name="walk">
		/// the TreeWalk to walk through. Must have exactly two trees and
		/// when
		/// <code>includeTrees</code>
		/// parameter is
		/// <code>true</code>
		/// it can't
		/// be recursive.
		/// </param>
		/// <param name="includeTrees">include tree object's.</param>
		/// <returns>headers describing the changed files.</returns>
		/// <exception cref="System.IO.IOException">the repository cannot be accessed.</exception>
		/// <exception cref="System.ArgumentException">
		/// when
		/// <code>includeTrees</code>
		/// is true and given TreeWalk is
		/// recursive. Or when given TreeWalk doesn't have exactly two
		/// trees
		/// </exception>
		public static IList<NGit.Diff.DiffEntry> Scan(TreeWalk walk, bool includeTrees)
		{
			if (walk.TreeCount != 2)
			{
				throw new ArgumentException(JGitText.Get().treeWalkMustHaveExactlyTwoTrees);
			}
			if (includeTrees && walk.Recursive)
			{
				throw new ArgumentException(JGitText.Get().cannotBeRecursiveWhenTreesAreIncluded);
			}
			IList<NGit.Diff.DiffEntry> r = new AList<NGit.Diff.DiffEntry>();
			MutableObjectId idBuf = new MutableObjectId();
			while (walk.Next())
			{
				NGit.Diff.DiffEntry entry = new NGit.Diff.DiffEntry();
				walk.GetObjectId(idBuf, 0);
				entry.oldId = AbbreviatedObjectId.FromObjectId(idBuf);
				walk.GetObjectId(idBuf, 1);
				entry.newId = AbbreviatedObjectId.FromObjectId(idBuf);
				entry.oldMode = walk.GetFileMode(0);
				entry.newMode = walk.GetFileMode(1);
				entry.newPath = entry.oldPath = walk.PathString;
				if (entry.oldMode == FileMode.MISSING)
				{
					entry.oldPath = NGit.Diff.DiffEntry.DEV_NULL;
					entry.changeType = DiffEntry.ChangeType.ADD;
					r.AddItem(entry);
				}
				else
				{
					if (entry.newMode == FileMode.MISSING)
					{
						entry.newPath = NGit.Diff.DiffEntry.DEV_NULL;
						entry.changeType = DiffEntry.ChangeType.DELETE;
						r.AddItem(entry);
					}
					else
					{
						if (!entry.oldId.Equals(entry.newId))
						{
							entry.changeType = DiffEntry.ChangeType.MODIFY;
							if (RenameDetector.SameType(entry.oldMode, entry.newMode))
							{
								r.AddItem(entry);
							}
							else
							{
								Sharpen.Collections.AddAll(r, BreakModify(entry));
							}
						}
						else
						{
							if (entry.oldMode != entry.newMode)
							{
								entry.changeType = DiffEntry.ChangeType.MODIFY;
								r.AddItem(entry);
							}
						}
					}
				}
				if (includeTrees && walk.IsSubtree)
				{
					walk.EnterSubtree();
				}
			}
			return r;
		}

		internal static NGit.Diff.DiffEntry Add(string path, AnyObjectId id)
		{
			NGit.Diff.DiffEntry e = new NGit.Diff.DiffEntry();
			e.oldId = A_ZERO;
			e.oldMode = FileMode.MISSING;
			e.oldPath = DEV_NULL;
			e.newId = AbbreviatedObjectId.FromObjectId(id);
			e.newMode = FileMode.REGULAR_FILE;
			e.newPath = path;
			e.changeType = DiffEntry.ChangeType.ADD;
			return e;
		}

		internal static NGit.Diff.DiffEntry Delete(string path, AnyObjectId id)
		{
			NGit.Diff.DiffEntry e = new NGit.Diff.DiffEntry();
			e.oldId = AbbreviatedObjectId.FromObjectId(id);
			e.oldMode = FileMode.REGULAR_FILE;
			e.oldPath = path;
			e.newId = A_ZERO;
			e.newMode = FileMode.MISSING;
			e.newPath = DEV_NULL;
			e.changeType = DiffEntry.ChangeType.DELETE;
			return e;
		}

		internal static NGit.Diff.DiffEntry Modify(string path)
		{
			NGit.Diff.DiffEntry e = new NGit.Diff.DiffEntry();
			e.oldMode = FileMode.REGULAR_FILE;
			e.oldPath = path;
			e.newMode = FileMode.REGULAR_FILE;
			e.newPath = path;
			e.changeType = DiffEntry.ChangeType.MODIFY;
			return e;
		}

		/// <summary>Breaks apart a DiffEntry into two entries, one DELETE and one ADD.</summary>
		/// <remarks>Breaks apart a DiffEntry into two entries, one DELETE and one ADD.</remarks>
		/// <param name="entry">the DiffEntry to break apart.</param>
		/// <returns>
		/// a list containing two entries. Calling
		/// <see cref="GetChangeType()">GetChangeType()</see>
		/// on the first entry will return ChangeType.DELETE. Calling it on
		/// the second entry will return ChangeType.ADD.
		/// </returns>
		internal static IList<NGit.Diff.DiffEntry> BreakModify(NGit.Diff.DiffEntry entry)
		{
			NGit.Diff.DiffEntry del = new NGit.Diff.DiffEntry();
			del.oldId = entry.GetOldId();
			del.oldMode = entry.GetOldMode();
			del.oldPath = entry.GetOldPath();
			del.newId = A_ZERO;
			del.newMode = FileMode.MISSING;
			del.newPath = NGit.Diff.DiffEntry.DEV_NULL;
			del.changeType = DiffEntry.ChangeType.DELETE;
			NGit.Diff.DiffEntry add = new NGit.Diff.DiffEntry();
			add.oldId = A_ZERO;
			add.oldMode = FileMode.MISSING;
			add.oldPath = NGit.Diff.DiffEntry.DEV_NULL;
			add.newId = entry.GetNewId();
			add.newMode = entry.GetNewMode();
			add.newPath = entry.GetNewPath();
			add.changeType = DiffEntry.ChangeType.ADD;
			return Arrays.AsList(del, add);
		}

		internal static NGit.Diff.DiffEntry Pair(DiffEntry.ChangeType changeType, NGit.Diff.DiffEntry
			 src, NGit.Diff.DiffEntry dst, int score)
		{
			NGit.Diff.DiffEntry r = new NGit.Diff.DiffEntry();
			r.oldId = src.oldId;
			r.oldMode = src.oldMode;
			r.oldPath = src.oldPath;
			r.newId = dst.newId;
			r.newMode = dst.newMode;
			r.newPath = dst.newPath;
			r.changeType = changeType;
			r.score = score;
			return r;
		}

		/// <summary>File name of the old (pre-image).</summary>
		/// <remarks>File name of the old (pre-image).</remarks>
		protected internal string oldPath;

		/// <summary>File name of the new (post-image).</summary>
		/// <remarks>File name of the new (post-image).</remarks>
		protected internal string newPath;

		/// <summary>Old mode of the file, if described by the patch, else null.</summary>
		/// <remarks>Old mode of the file, if described by the patch, else null.</remarks>
		protected internal FileMode oldMode;

		/// <summary>New mode of the file, if described by the patch, else null.</summary>
		/// <remarks>New mode of the file, if described by the patch, else null.</remarks>
		protected internal FileMode newMode;

		/// <summary>General type of change indicated by the patch.</summary>
		/// <remarks>General type of change indicated by the patch.</remarks>
		protected internal DiffEntry.ChangeType changeType;

		/// <summary>
		/// Similarity score if
		/// <see cref="changeType">changeType</see>
		/// is a copy or rename.
		/// </summary>
		protected internal int score;

		/// <summary>ObjectId listed on the index line for the old (pre-image)</summary>
		protected internal AbbreviatedObjectId oldId;

		/// <summary>ObjectId listed on the index line for the new (post-image)</summary>
		protected internal AbbreviatedObjectId newId;

		/// <summary>Get the old name associated with this file.</summary>
		/// <remarks>
		/// Get the old name associated with this file.
		/// <p>
		/// The meaning of the old name can differ depending on the semantic meaning
		/// of this patch:
		/// <ul>
		/// <li><i>file add</i>: always <code>/dev/null</code></li>
		/// <li><i>file modify</i>: always
		/// <see cref="GetNewPath()">GetNewPath()</see>
		/// </li>
		/// <li><i>file delete</i>: always the file being deleted</li>
		/// <li><i>file copy</i>: source file the copy originates from</li>
		/// <li><i>file rename</i>: source file the rename originates from</li>
		/// </ul>
		/// </remarks>
		/// <returns>old name for this file.</returns>
		public virtual string GetOldPath()
		{
			return oldPath;
		}

		/// <summary>Get the new name associated with this file.</summary>
		/// <remarks>
		/// Get the new name associated with this file.
		/// <p>
		/// The meaning of the new name can differ depending on the semantic meaning
		/// of this patch:
		/// <ul>
		/// <li><i>file add</i>: always the file being created</li>
		/// <li><i>file modify</i>: always
		/// <see cref="GetOldPath()">GetOldPath()</see>
		/// </li>
		/// <li><i>file delete</i>: always <code>/dev/null</code></li>
		/// <li><i>file copy</i>: destination file the copy ends up at</li>
		/// <li><i>file rename</i>: destination file the rename ends up at/li&gt;
		/// </ul>
		/// </remarks>
		/// <returns>new name for this file.</returns>
		public virtual string GetNewPath()
		{
			return newPath;
		}

		/// <summary>Get the path associated with this file.</summary>
		/// <remarks>Get the path associated with this file.</remarks>
		/// <param name="side">which path to obtain.</param>
		/// <returns>name for this file.</returns>
		public virtual string GetPath(DiffEntry.Side side)
		{
			return side == DiffEntry.Side.OLD ? GetOldPath() : GetNewPath();
		}

		/// <returns>the old file mode, if described in the patch</returns>
		public virtual FileMode GetOldMode()
		{
			return oldMode;
		}

		/// <returns>the new file mode, if described in the patch</returns>
		public virtual FileMode GetNewMode()
		{
			return newMode;
		}

		/// <summary>Get the mode associated with this file.</summary>
		/// <remarks>Get the mode associated with this file.</remarks>
		/// <param name="side">which mode to obtain.</param>
		/// <returns>the mode.</returns>
		public virtual FileMode GetMode(DiffEntry.Side side)
		{
			return side == DiffEntry.Side.OLD ? GetOldMode() : GetNewMode();
		}

		/// <returns>
		/// the type of change this patch makes on
		/// <see cref="GetNewPath()">GetNewPath()</see>
		/// 
		/// </returns>
		public virtual DiffEntry.ChangeType GetChangeType()
		{
			return changeType;
		}

		/// <returns>
		/// similarity score between
		/// <see cref="GetOldPath()">GetOldPath()</see>
		/// and
		/// <see cref="GetNewPath()">GetNewPath()</see>
		/// if
		/// <see cref="GetChangeType()">GetChangeType()</see>
		/// is
		/// <see cref="ChangeType.COPY">ChangeType.COPY</see>
		/// or
		/// <see cref="ChangeType.RENAME">ChangeType.RENAME</see>
		/// .
		/// </returns>
		public virtual int GetScore()
		{
			return score;
		}

		/// <summary>Get the old object id from the <code>index</code>.</summary>
		/// <remarks>Get the old object id from the <code>index</code>.</remarks>
		/// <returns>the object id; null if there is no index line</returns>
		public virtual AbbreviatedObjectId GetOldId()
		{
			return oldId;
		}

		/// <summary>Get the new object id from the <code>index</code>.</summary>
		/// <remarks>Get the new object id from the <code>index</code>.</remarks>
		/// <returns>the object id; null if there is no index line</returns>
		public virtual AbbreviatedObjectId GetNewId()
		{
			return newId;
		}

		/// <summary>Get the object id.</summary>
		/// <remarks>Get the object id.</remarks>
		/// <param name="side">the side of the id to get.</param>
		/// <returns>the object id; null if there is no index line</returns>
		public virtual AbbreviatedObjectId GetId(DiffEntry.Side side)
		{
			return side == DiffEntry.Side.OLD ? GetOldId() : GetNewId();
		}

		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append("DiffEntry[");
			buf.Append(changeType);
			buf.Append(" ");
			switch (changeType)
			{
				case DiffEntry.ChangeType.ADD:
				{
					buf.Append(newPath);
					break;
				}

				case DiffEntry.ChangeType.COPY:
				{
					buf.Append(oldPath + "->" + newPath);
					break;
				}

				case DiffEntry.ChangeType.DELETE:
				{
					buf.Append(oldPath);
					break;
				}

				case DiffEntry.ChangeType.MODIFY:
				{
					buf.Append(oldPath);
					break;
				}

				case DiffEntry.ChangeType.RENAME:
				{
					buf.Append(oldPath + "->" + newPath);
					break;
				}
			}
			buf.Append("]");
			return buf.ToString();
		}
	}
}
