#region license
//
//	(C) 2005 - 2007 db4objects Inc. http://www.db4o.com
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;
using System.Collections;
using System.Collections.Generic;

using Mono.Cecil.Cil;

namespace Cecil.Decompiler.Cil {

	public class InstructionBlock : IEnumerable<Instruction>, IComparable<InstructionBlock> {

		static readonly InstructionBlock [] NoSuccessors = new InstructionBlock [0];

		int index;
		Instruction first;
		Instruction last;
		InstructionBlock [] successors = NoSuccessors;

		public int Index {
			get { return index; }
			internal set { index = value; }
		}

		public Instruction First {
			get { return first; }
			internal set { first = value; }
		}

		public Instruction Last {
			get { return last; }
			internal set { last = value; }

		}

		public InstructionBlock [] Successors {
			get { return successors; }
			internal set { successors = value; }
		}

		internal InstructionBlock (Instruction first)
		{
			if (first == null)
				throw new ArgumentNullException ("first");

			this.first = first;
		}

		public int CompareTo (InstructionBlock block)
		{
			return first.Offset - block.First.Offset;
		}

		public IEnumerator<Instruction> GetEnumerator ()
		{
			var instruction = first;
			while (true) {
				yield return instruction;

				if (instruction == last)
					yield break;

				instruction = instruction.Next;
			}
		}

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
	}
}
