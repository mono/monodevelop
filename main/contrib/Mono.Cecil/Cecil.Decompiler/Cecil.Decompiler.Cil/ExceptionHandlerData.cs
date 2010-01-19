#region license
//
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
using System.Collections.Generic;

using Mono.Cecil;

namespace Cecil.Decompiler.Cil {

	public class CatchHandlerData {

		public readonly TypeReference Type;
		public readonly BlockRange Range;

		public CatchHandlerData (TypeReference type, BlockRange range)
		{
			Type = type;
			Range = range;
		}
	}

	public class ExceptionHandlerData : IComparable<ExceptionHandlerData> {

		BlockRange try_range;
		List<CatchHandlerData> catches = new List<CatchHandlerData> ();
		BlockRange finally_range;
		BlockRange fault_range;

		public BlockRange TryRange {
			get { return try_range; }
			set { try_range = value; }
		}

		public List<CatchHandlerData> Catches {
			get { return catches; }
		}

		public BlockRange FinallyRange {
			get { return finally_range; }
			set { finally_range = value; }
		}

		public BlockRange FaultRange {
			get { return fault_range; }
			set { fault_range = value; }
		}

		public ExceptionHandlerData (BlockRange try_range)
		{
			this.try_range = try_range;
		}

		public int CompareTo (ExceptionHandlerData data)
		{
			return try_range.Start.First.Offset - data.try_range.Start.First.Offset;
		}
	}
}
