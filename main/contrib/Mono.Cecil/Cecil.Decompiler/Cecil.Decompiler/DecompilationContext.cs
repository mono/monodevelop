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

using Mono.Cecil;
using Mono.Cecil.Cil;

using Cecil.Decompiler.Cil;

namespace Cecil.Decompiler {

	public class DecompilationContext {

		MethodDefinition method;
		MethodBody body;
		VariableDefinitionCollection variables;
		ControlFlowGraph cfg;

		public MethodDefinition Method {
			get { return method; }
		}

		public MethodBody Body {
			get { return body; }
		}

		public VariableDefinitionCollection Variables {
			get { return variables; }
		}

		public ControlFlowGraph ControlFlowGraph {
			get { return cfg; }
		}

		internal DecompilationContext (MethodBody body, ControlFlowGraph cfg)
		{
			this.body = body;
			this.method = body.Method;
			this.variables = CloneCollection (body.Variables);
			this.cfg = cfg;
		}

		public void RemoveVariable (VariableReference reference)
		{
			RemoveVariable (reference.Resolve ());
		}

		public void RemoveVariable (VariableDefinition variable)
		{
			var index = variables.IndexOf (variable);
			if (index == -1)
				return;

			variables.RemoveAt (index);
		}

		static VariableDefinitionCollection CloneCollection (VariableDefinitionCollection variables)
		{
			var collection = new VariableDefinitionCollection (variables.Container);

			foreach (VariableDefinition variable in variables)
				collection.Add (variable);

			return collection;
		}
	}
}
