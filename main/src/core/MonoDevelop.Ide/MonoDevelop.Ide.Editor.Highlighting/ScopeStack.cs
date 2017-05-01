//
// ScopeStack.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Collections;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public sealed class ScopeStack : IEnumerable<string>
	{
		public static readonly ScopeStack Empty = new ScopeStack (null, ImmutableStack<string>.Empty, 0, null);

		public string FirstElement {
			get;
			private set;
		}

		public bool IsEmpty {
			get {
				return stack.IsEmpty;
			}
		}

		public int Count {
			get;
			private set;
		}

		ImmutableStack<string> stack;
		ScopeStack parent;

		public ScopeStack (string first)
		{
			FirstElement = first;
			stack = ImmutableStack<string>.Empty.Push (first);
			parent = ScopeStack.Empty;
			Count = 1;
		}

		// The reasoning for having a constructor which takes a parent is that we want to make allocations
		// only onthe less common operation - Push. Having the allocation of the parent ScopeStack happening
		// on Pop, rather than retaining the one on Push, would yield an allocation hot loop, given that:
		// We already allocate it once on push.
		// We pass the same scope stack multiple times.
		ScopeStack (string first, ImmutableStack<string> immutableStack, int count, ScopeStack parent)
		{
			this.FirstElement = first;
			this.stack = immutableStack;
			this.Count = count;
			this.parent = parent;
		}

		public ScopeStack Push (string item)
		{
			return new ScopeStack (FirstElement, stack.Push (item), Count + 1, this);
		}

		public ScopeStack Pop ()
		{
			if (parent == null)
				throw new InvalidOperationException ("ScopeStack is empty");
			return parent;
		}

		public string Peek ()
		{
			return stack.Peek ();
		}

		public ImmutableStack<string>.Enumerator GetEnumerator ()
		{
			return stack.GetEnumerator ();
		}

		IEnumerator<string> IEnumerable<string>.GetEnumerator ()
		{			
			return ((IEnumerable<string>)stack).GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return ((IEnumerable<string>)stack).GetEnumerator ();
		}
	}
}
