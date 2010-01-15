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
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cecil.Decompiler.Cil {

	public class ControlFlowGraph {

		MethodBody body;
		InstructionBlock [] blocks;
		Dictionary<int, InstructionData> data;
		List<ExceptionHandlerData> exception_data;
		HashSet<int> exception_objects_offsets;

		public MethodBody MethodBody {
			get { return body; }
		}

		public InstructionBlock [] Blocks {
			get { return blocks; }
		}

		public ControlFlowGraph (
			MethodBody body,
			InstructionBlock [] blocks,
			Dictionary<int, InstructionData> instructionData,
			List<ExceptionHandlerData> exception_data,
			HashSet<int> exception_objects_offsets)
		{
			this.body = body;
			this.blocks = blocks;
			this.data = instructionData;
			this.exception_data = exception_data;
			this.exception_objects_offsets = exception_objects_offsets;
		}

		public InstructionData GetData (Instruction instruction)
		{
			return data [instruction.Offset];
		}

		public ExceptionHandlerData [] GetExceptionData ()
		{
			return exception_data.ToArray ();
		}

		public bool HasExceptionObject (int offset)
		{
			if (exception_objects_offsets == null)
				return false;

			return exception_objects_offsets.Contains (offset);
		}

		public static ControlFlowGraph Create (MethodDefinition method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");
			if (!method.HasBody)
				throw new ArgumentException ();

			var builder = new ControlFlowGraphBuilder (method);
			return builder.CreateGraph ();
		}
	}
}
