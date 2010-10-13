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

using System.Collections.Generic;
using NGit.Diff;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>
	/// Specialized list of
	/// <see cref="Edit">Edit</see>
	/// s in a document.
	/// </summary>
	public class EditList : AbstractList<Edit>
	{
		/// <summary>Construct an edit list containing a single edit.</summary>
		/// <remarks>Construct an edit list containing a single edit.</remarks>
		/// <param name="edit">the edit to return in the list.</param>
		/// <returns>
		/// list containing only
		/// <code>edit</code>
		/// .
		/// </returns>
		public static NGit.Diff.EditList Singleton(Edit edit)
		{
			NGit.Diff.EditList res = new NGit.Diff.EditList(1);
			res.AddItem(edit);
			return res;
		}

		private readonly AList<Edit> container;

		/// <summary>Create a new, empty edit list.</summary>
		/// <remarks>Create a new, empty edit list.</remarks>
		public EditList()
		{
			container = new AList<Edit>();
		}

		/// <summary>Create an empty edit list with the specified capacity.</summary>
		/// <remarks>Create an empty edit list with the specified capacity.</remarks>
		/// <param name="capacity">
		/// the initial capacity of the edit list. If additional edits are
		/// added to the list, it will be grown to support them.
		/// </param>
		public EditList(int capacity)
		{
			container = new AList<Edit>(capacity);
		}

		public override int Count
		{
			get
			{
				return container.Count;
			}
		}

		public override Edit Get(int index)
		{
			return container[index];
		}

		public override Edit Set(int index, Edit element)
		{
			return container.Set(index, element);
		}

		public override void Add(int index, Edit element)
		{
			container.Add(index, element);
		}

		public override bool AddAll<_T0>(ICollection<_T0> c)
		{
			return Sharpen.Collections.AddAll(container, c);
		}

		public override Edit Remove(int index)
		{
			return container.Remove(index);
		}

		public override int GetHashCode()
		{
			return container.GetHashCode();
		}

		public override bool Equals(object o)
		{
			if (o is NGit.Diff.EditList)
			{
				return container.Equals(((NGit.Diff.EditList)o).container);
			}
			return false;
		}

		public override string ToString()
		{
			return "EditList" + container.ToString();
		}
	}
}
