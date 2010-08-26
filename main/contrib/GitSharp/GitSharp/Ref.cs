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
using GitSharp.Core;
using ObjectId = GitSharp.Core.ObjectId;
using CoreRef = GitSharp.Core.Ref;
using CoreCommit = GitSharp.Core.Commit;
using CoreTree = GitSharp.Core.Tree;
using CoreRepository = GitSharp.Core.Repository;

namespace GitSharp
{

	/// <summary>
	/// Ref is a named symbolic reference that is a pointing to a specific git object. It is not resolved
	/// until you explicitly retrieve the link target. The Target is not cached.
	/// </summary>
	public class Ref : IReferenceObject
	{
		internal Repository _repo;
		//private _internal_ref;

		public Ref(Repository repo, string name)
		{
			_repo = repo;
			Name = name;
		}

		internal Ref(Repository repo, CoreRef @ref)
			: this(repo, @ref.Name)
		{
		}

		public string Name
		{
			get;
			private set;
		}

		/// <summary>
		/// Resolve the symbolic reference and return the object that it is currently pointing at. Target is not cached
		/// in order to match the behavior of a real git ref.
		/// </summary>
		public AbstractObject Target
		{
			get
			{
				var id = _repo._internal_repo.Resolve(Name);
				if (id == null)
					return null;
				return AbstractObject.Wrap(_repo, id);
			}
		}

		public bool IsBranch
		{
			get
			{
				IDictionary<string, CoreRef> branches = _repo._internal_repo._refDb.getRefs(Constants.R_HEADS);
				return branches.ContainsKey(Name);
			}
		}

		/// <summary>
		/// Updates this ref by linking it to the given ref's target.
		/// </summary>
		/// <param name="reference">The ref this ref shall reference.</param>
		public void Update(Ref reference)
		{
			Update(reference.Target);
		}

		/// <summary>
		/// Updates this ref by forwarding it to the given object.
		/// </summary>
		/// <param name="reference">The ref this object shall reference.</param>
		public void Update(AbstractObject reference)
		{
			var db = _repo._internal_repo;
			var updateRef = db.UpdateRef(this.Name);
			updateRef.NewObjectId = reference._id;
			updateRef.IsForceUpdate = true;
			updateRef.update();
			//db.WriteSymref(Name, other.Name);
		}

		public static void Update(string name, AbstractObject reference)
		{
			new Ref(reference.Repository, name).Update(reference);
		}

		/// <summary>
		/// Check validity of a ref name. It must not contain a character that has
		/// a special meaning in a Git object reference expression. Some other
		/// dangerous characters are also excluded.
		/// </summary>
		/// <param name="refName"></param>
		/// <returns>
		/// Returns true if <paramref name="refName"/> is a valid ref name.
		/// </returns>
		public static bool IsValidName(string refName)
		{
			return CoreRepository.IsValidRefName(refName);
		}

		#region Equality overrides

		public override bool Equals(object obj)
		{
			if (obj is Ref)
			{
				var other = obj as Ref;
				return _repo._internal_repo.Resolve(Name) == _repo._internal_repo.Resolve(other.Name);
			}
			else
				return false;
		}

		public static bool operator ==(Ref self, object other)
		{
			return self.Equals(other);
		}

		public static bool operator !=(Ref self, object other)
		{
			return !self.Equals(other);
		}

		public override int GetHashCode()
		{
			var id = _repo._internal_repo.Resolve(Name);
			if (id != null)
				return id.GetHashCode();
			return base.GetHashCode();
		}

		#endregion

		public override string ToString()
		{
			return "Ref[" + Name + "]";
		}

	}
}
