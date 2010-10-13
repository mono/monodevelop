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

using System.Text;
using NGit;
using Sharpen;

namespace NGit
{
	/// <summary>
	/// A reference that indirectly points at another
	/// <see cref="Ref">Ref</see>
	/// .
	/// <p>
	/// A symbolic reference always derives its current value from the target
	/// reference.
	/// </summary>
	public class SymbolicRef : Ref
	{
		private readonly string name;

		private readonly Ref target;

		/// <summary>Create a new ref pairing.</summary>
		/// <remarks>Create a new ref pairing.</remarks>
		/// <param name="refName">name of this ref.</param>
		/// <param name="target">the ref we reference and derive our value from.</param>
		public SymbolicRef(string refName, Ref target)
		{
			this.name = refName;
			this.target = target;
		}

		public virtual string GetName()
		{
			return name;
		}

		public virtual bool IsSymbolic()
		{
			return true;
		}

		public virtual Ref GetLeaf()
		{
			Ref dst = GetTarget();
			while (dst.IsSymbolic())
			{
				dst = dst.GetTarget();
			}
			return dst;
		}

		public virtual Ref GetTarget()
		{
			return target;
		}

		public virtual ObjectId GetObjectId()
		{
			return GetLeaf().GetObjectId();
		}

		public virtual RefStorage GetStorage()
		{
			return RefStorage.LOOSE;
		}

		public virtual ObjectId GetPeeledObjectId()
		{
			return GetLeaf().GetPeeledObjectId();
		}

		public virtual bool IsPeeled()
		{
			return GetLeaf().IsPeeled();
		}

		public override string ToString()
		{
			StringBuilder r = new StringBuilder();
			r.Append("SymbolicRef[");
			Ref cur = this;
			while (cur.IsSymbolic())
			{
				r.Append(cur.GetName());
				r.Append(" -> ");
				cur = cur.GetTarget();
			}
			r.Append(cur.GetName());
			r.Append('=');
			r.Append(ObjectId.ToString(cur.GetObjectId()));
			r.Append("]");
			return r.ToString();
		}
	}
}
