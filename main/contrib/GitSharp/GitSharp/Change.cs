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

namespace GitSharp
{

	public enum ChangeType
	{
		Added, Deleted, Modified, TypeChanged, Renamed, Copied
	}

	/// <summary>
	/// Represents a change of a single file between two commits. Use Commit.Diff to get a list of Change objects.
	/// </summary>
	public class Change
	{

		/// <summary>
		/// The commit that serves as reference for this comparison. The change reflects the difference of the other commit against this ReferenceCommit.
		/// </summary>
		public Commit ReferenceCommit
		{
			get;
			internal set;
		}

		/// <summary>
		/// The commit which is compared against the ReferenceCommit.
		/// </summary>
		public Commit ComparedCommit
		{
			get;
			internal set;
		}

		/// <summary>
		/// The kind of change (Added, Modified, Deleted, etc. )
		/// </summary>
		public ChangeType ChangeType { get; internal set; }

		/// <summary>
		/// The revision of the file from the ReferenceCommit. It may be null in some cases i.e. for ChangeType.Added
		/// </summary>
		public AbstractObject ReferenceObject { get; internal set; }

		/// <summary>
		/// The revision of the file from the ComparedCommit. It may be null in some cases i.e. for ChangeType.Removed
		/// </summary>
		public AbstractObject ComparedObject { get; internal set; }

		/// <summary>
		/// The file (i.e. Blob) this Change is according to.
		/// Always returns a non-null revision of the file, no matter what kind of change. It normally returns the ComparedCommit's version of the changed 
		/// object except for ChangeType.Removed where it returns the ReferenceCommit's version of the object.
		/// 
		/// This property is designed to release the calling code from null checking and revision selection and may be especially useful for GUI bindings.
		/// </summary>
		public AbstractObject ChangedObject
		{
			get
			{
				if (ComparedObject != null)
					return ComparedObject;
				else
					return ReferenceObject;
			}
		}

		/// <summary>
		/// The filepath of the ChangedObject
		/// </summary>
		public string Path { get; internal set; }

		/// <summary>
		/// The filename of the ChangedObject
		/// </summary>
		public string Name { get; internal set; }

		/// <summary>
		/// Unix file permissions of the ReferenceCommit's version of the object
		/// </summary>
		public int ReferencePermissions
		{
			get;
			internal set;
		}

		/// <summary>
		/// Unix file permissions of the ComparedCommit's version of the object
		/// </summary>
		public int ComparedPermissions { get; internal set; }

		/// <summary>
		/// Returns ReferenceCommit and ComparedCommit as array
		/// </summary>
		public Commit[] Commits
		{
			get
			{
				return new Commit[] { ReferenceCommit, ComparedCommit };
			}
		}

		/// <summary>
		/// Returns ReferenceObject and ComparedObject as array
		/// </summary>
		public AbstractObject[] Objects
		{
			get
			{
				return new AbstractObject[] { ReferenceObject, ComparedObject };
			}
		}

		/// <summary>
		/// Returns ReferenceObject's and ComparedObject's permissions as array
		/// </summary>
		public int[] Permissions
		{
			get
			{
				return new int[] { ReferencePermissions, ComparedPermissions };
			}
		}

		public override string ToString()
		{
			return string.Format("{0} [{1}]", ChangeType, Path);
		}

	}

}
