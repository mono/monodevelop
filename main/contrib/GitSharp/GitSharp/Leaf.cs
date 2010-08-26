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
using FileTreeEntry = GitSharp.Core.FileTreeEntry;

namespace GitSharp
{

	/// <summary>
	/// Leaf represents a tracked file in a directory tracked by git.
	/// </summary>
	public class Leaf : AbstractTreeNode
	{
		internal Leaf(Repository repo, FileTreeEntry entry)
			: base(repo, entry.Id)
		{
			_internal_file_tree_entry = entry;
		}

		private FileTreeEntry _internal_file_tree_entry;

		/// <summary>
		/// True if the file is executable (unix).
		/// </summary>
		public bool IsExecutable
		{
			get
			{
				return _internal_file_tree_entry.IsExecutable;
			}
		}

		/// <summary>
		/// The file name
		/// </summary>
		public override string Name
		{
			get
			{
				return _internal_file_tree_entry.Name;
			}
		}

		/// <summary>
		/// The full path relative to repostiory root
		/// </summary>
		public override string Path
		{
			get
			{
				return _internal_file_tree_entry.FullName;
			}
		}

		/// <summary>
		/// The unix file permissions.
		/// 
		/// Todo: model this with a permission object
		/// </summary>
		public override int Permissions
		{
			get
			{
				return _internal_file_tree_entry.Mode.Bits;
			}
		}

		/// <summary>
		/// The parent <see cref="Tree"/>.
		/// </summary>
		public override Tree Parent
		{
			get
			{
				return new Tree(_repo, _internal_file_tree_entry.Parent);
			}
		}

		/// <summary>
		/// Return a <see cref="Blob"/> containing the data of this file
		/// </summary>
		public Blob Blob
		{
			get { return new Blob(_repo, _id); }
		}

		public static implicit operator Blob(Leaf self)
		{
			return self.Blob;
		}

		public string Data
		{
			get { return Blob.Data; }
		}

		public byte[] RawData
		{
			get { return Blob.RawData; }
		}
	}
}
