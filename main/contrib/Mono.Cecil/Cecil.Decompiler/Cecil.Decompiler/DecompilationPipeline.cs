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
using Mono.Cecil.Cil;

using Cecil.Decompiler.Ast;
using Cecil.Decompiler.Cil;
using Cecil.Decompiler.Steps;

namespace Cecil.Decompiler {

	public class DecompilationPipeline {

		DecompilationContext context;
		BlockStatement body_block;

		IEnumerable<IDecompilationStep> steps;

		public DecompilationContext Context {
			get { return context; }
		}

		public BlockStatement Body {
			get { return body_block; }
		}

		public DecompilationPipeline (params IDecompilationStep [] steps)
			: this (steps as IEnumerable<IDecompilationStep>)
		{
		}

		public DecompilationPipeline (IEnumerable<IDecompilationStep> steps)
		{
			this.steps = steps;
		}

		public void Run (MethodBody body)
		{
			this.context = new DecompilationContext (body, ControlFlowGraph.Create (body.Method));
			var block = new BlockStatement ();

			foreach (var step in steps)
				block = step.Process (context, block);

			body_block = block;
		}
	}
}
