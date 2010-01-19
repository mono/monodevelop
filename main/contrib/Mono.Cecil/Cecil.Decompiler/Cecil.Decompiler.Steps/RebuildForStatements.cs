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

using Mono.Cecil.Cil;

using Cecil.Decompiler.Ast;

namespace Cecil.Decompiler.Steps {

	class RebuildForStatements : Ast.BaseCodeVisitor, IDecompilationStep {

		public static readonly IDecompilationStep Instance = new RebuildForStatements ();

	    public override void VisitBlockStatement (BlockStatement node)
		{
			ProcessBlock (node);

			foreach (var statement in node.Statements)
				Visit (statement);
		}

		void ProcessBlock (BlockStatement node)
		{
			var matcher = new ForMatcher ();
			matcher.Visit (node);

			if (!matcher.Match)
				return;

			var index = node.Statements.IndexOf (matcher.Initializer);
			node.Statements.RemoveAt (index); // initializer
			node.Statements.RemoveAt (index); // while
			node.Statements.Insert (index, matcher.For);
		}

		class ForMatcher : Matcher {

			enum State {
				Begin,
				Initializer,
				While,
				Condition,
				Increment,
			}

			State state = State.Begin;

			Statement initializer;
			Expression condition;
			Statement increment;

			ForStatement @for;

			VariableReference variable;

			public Statement Initializer {
				get { return initializer; }
			}

			public ForStatement For {
				get { return @for; }
			}

			public override void VisitExpressionStatement (ExpressionStatement node)
			{
				switch (state) {
				case State.Begin:
					if (!TryGetAssignedVariable (node, out variable))
						break;

					initializer = node;
					state = State.Initializer;
					return;
				case State.Condition:
					VariableReference candidate;
					if (!TryGetAssignedVariable (node, out candidate))
						break;

					if (variable != candidate)
						break;

					state = State.Increment;
					increment = node;
					Match = true;
					break;
				}

				Continue = false;
			}

			static bool TryGetAssignedVariable (ExpressionStatement node, out VariableReference variable)
			{
				variable = null;

				var assign = node.Expression as AssignExpression;
				if (assign == null)
					return false;

				var variable_reference = assign.Target as VariableReferenceExpression;
				if (variable_reference == null)
					return false;

				variable = variable_reference.Variable;
				return true;
			}

			public override void VisitWhileStatement (WhileStatement node)
			{
				switch (state) {
				case State.Initializer:
					if (!VariableMatcher.FindVariableInExpression (variable, node.Condition))
						break;

					state = State.Condition;
					condition = node.Condition;

					var body = node.Body;

					if (body.Statements.Count == 0)
						break;

					Visit (body.Statements [body.Statements.Count - 1]);

					if (!Match)
						break;

					CreateForStatement (body);
					break;
				}

				Continue = false;
			}

			void CreateForStatement (BlockStatement body)
			{
				@for = new ForStatement (
					initializer,
					condition,
					increment,
					new BlockStatement ());

				for (int i = 0; i < body.Statements.Count - 1; i++)
					@for.Body.Statements.Add (body.Statements [i]);
			}
		}

		public BlockStatement Process (DecompilationContext context, BlockStatement body)
		{
			Visit (body);
			return body;
		}
	}
}
