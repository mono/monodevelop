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

using ObjectId = GitSharp.Core.ObjectId;
using CoreRef = GitSharp.Core.Ref;
using CoreCommit = GitSharp.Core.Commit;
using CoreTree = GitSharp.Core.Tree;
using CoreTag = GitSharp.Core.Tag;

namespace GitSharp
{
	/// <summary>
	/// Represents a git tag.
	/// </summary>
	public class Tag : AbstractObject, IReferenceObject
	{

		public Tag(Repository repo, string name)
			: base(repo, name)
		{
			_name = name;
		}

		private string _name; // <--- need the name for resolving purposes only. once the internal tag is resolved, this field is not used any more.

		internal Tag(Repository repo, CoreRef @ref)
			: base(repo, @ref.ObjectId)
		{
			_name = @ref.Name;
		}

		internal Tag(Repository repo, CoreTag internal_tag)
			: base(repo, internal_tag.Id)
		{
			_internal_tag = internal_tag;
		}

		internal Tag(Repository repo, ObjectId id, string name)
			: base(repo, id)
		{
			_name = name;
		}

		private CoreTag _internal_tag;

		private CoreTag InternalTag
		{
			get
			{
				if (_internal_tag == null)
					try
					{
						_internal_tag = _repo._internal_repo.MapTag(_name, _id);
					}
					catch (Exception)
					{
						// the object is invalid. however, we can not allow exceptions here because they would not be expected.
					}
				return _internal_tag;
			}
		}

		public override bool IsTag
		{
			get
			{
				if (InternalTag == null)
					return false;
				return true;
			}
		}

		/// <summary>
		/// The tag name.
		/// </summary>
		public string Name
		{
			get
			{
				if (InternalTag == null)
					return _name;
				return InternalTag.TagName;
			}
		}

		/// <summary>
		/// The object that has been tagged.
		/// </summary>
		public AbstractObject Target
		{
			get
			{
				if (InternalTag == null)
					return null;
				if (InternalTag.TagId == InternalTag.Id) // <--- it can happen!
					return this;
				return AbstractObject.Wrap(_repo, InternalTag.Id);
			}
		}

		public override string ToString()
		{
			return "Tag[" + ShortHash + "]";
		}
	}
}
