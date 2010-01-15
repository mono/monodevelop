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

using Mono.Cecil.Cil;

using Cecil.Decompiler.Ast;

namespace Cecil.Decompiler.Steps {

	public class DeclareVariablesOnFirstAssignment : BaseCodeTransformer, IDecompilationStep {

		public static readonly IDecompilationStep Instance = new DeclareVariablesOnFirstAssignment ();

		DecompilationContext context;
		HashSet<VariableDefinition> not_assigned = new HashSet<VariableDefinition> ();

		public override ICodeNode VisitVariableReferenceExpression (VariableReferenceExpression node)
		{
			var variable = (VariableDefinition) node.Variable;

			if (!TryDiscardVariable (variable))
				return node;

			return new VariableDeclarationExpression (variable);
		}

		public override ICodeNode VisitVariableDeclarationExpression (VariableDeclarationExpression node)
		{
			TryDiscardVariable (node.Variable);
			
			return node;
		}

		private bool TryDiscardVariable (VariableDefinition variable)
		{
			if (!not_assigned.Contains (variable))
				return false;

			RemoveVariable (variable);
			return true;
		}

		void RemoveVariable (VariableDefinition variable)
		{
			context.RemoveVariable (variable);
			not_assigned.Remove (variable);
		}

		public BlockStatement Process (DecompilationContext context, BlockStatement block)
		{
			this.context = context;
			PopulateNotAssigned ();
			return (BlockStatement) VisitBlockStatement (block);
		}

		void PopulateNotAssigned ()
		{
			not_assigned.Clear ();
			foreach (VariableDefinition variable in context.Variables)
				not_assigned.Add (variable);
		}
	}
}
