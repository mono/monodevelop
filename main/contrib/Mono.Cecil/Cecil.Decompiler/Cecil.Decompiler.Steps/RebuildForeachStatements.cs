using System.Linq;

using Cecil.Decompiler.Ast;

using Mono.Cecil.Cil;

namespace Cecil.Decompiler.Steps {

	internal class RebuildForeachStatements : Ast.BaseCodeVisitor, IDecompilationStep {

		public static readonly IDecompilationStep Instance = new RebuildForeachStatements ();

		DecompilationContext context;

		public override void VisitBlockStatement (BlockStatement node)
		{
			ProcessBlock (node);

			foreach (var statement in node.Statements)
				Visit (statement);
		}

		void ProcessBlock (BlockStatement node)
		{
			for (int i = 0; i < node.Statements.ToList ().Count - 1; i++) {
				var matcher = new ForeachMatcher (node.Statements [i], node.Statements [i + 1]);
				if (!matcher.Match ())
					continue;
				context.RemoveVariable (matcher.Enumerator);
				node.Statements.RemoveAt (i); // enumerator declaration/assignment
				node.Statements.RemoveAt (i); // try
				node.Statements.Insert (i, matcher.Foreach);
				ProcessBlock (matcher.Foreach.Body);
			}
		}

		public BlockStatement Process (DecompilationContext context, BlockStatement body)
		{
			this.context = context;
			Visit (body);
			return body;
		}

		class ForeachMatcher {

			enum State {
				Begin,
				WhileBody,
				WhileCondition,
				IfBody
			}

			ForEachStatement @foreach;
            VariableReference enumerator;

			Expression source;
            BlockStatement while_body;
            VariableDefinition variable;

			State state;

			readonly Statement statement;
            readonly Statement next_statement;

			public ForEachStatement Foreach {
				get {
					@foreach = @foreach ?? new ForEachStatement (
					                       	new VariableDeclarationExpression (this.variable),
					                       	this.source,
					                       	this.while_body);
					return @foreach;
				}
			}

			public VariableReference Enumerator {
				get { return this.enumerator; }
			}

			public ForeachMatcher (Statement statement, Statement next_statement)
			{
				this.statement = statement;
				this.next_statement = next_statement;
			}

			internal bool Match ()
			{
				state = State.Begin;

				if (!VisitExpressionStatement (this.statement))
					return false;

				if (!VisitTryStatement (this.next_statement))
					return false;

				return true;
			}

			private bool VisitExpressionStatement (Statement node)
			{
				var expression_statement = node as ExpressionStatement;
				if (expression_statement == null)
					return false;

				switch (state) {
					case State.Begin:
					case State.WhileBody:
						if (!VisitAssignExpression (expression_statement.Expression))
							return false;
						break;

					case State.IfBody:
						if (!VisitMethodInvocationExpression (expression_statement.Expression))
							return false;
						break;
				}

				return true;
			}

			bool VisitAssignExpression (Expression node)
			{
				var assign_expression = node as AssignExpression;
				if (assign_expression == null)
					return false;

				if (!VisitVariableReferenceExpression (assign_expression.Target))
					return false;

				if (!VisitMethodInvocationExpression (assign_expression.Expression))
					return false;

				return true;
			}

			bool VisitTryStatement (Statement node)
			{
				var try_statement = node as TryStatement;
				if (try_statement == null)
					return false;

				if (!IsValidTryStatement (try_statement))
					return false;

				if (!VisitWhileStatement (try_statement.Try.Statements [0]))
					return false;

				if (!VisitIfStatement (try_statement.Finally.Statements [0]))
					return false;

				return true;
			}

			bool VisitIfStatement (Statement statement)
			{
				var if_statement = statement as IfStatement;
				if (if_statement == null || if_statement.Else != null || if_statement.Then.Statements.Count != 1)
					return false;

				var binary_expression = if_statement.Condition as BinaryExpression;
				if (binary_expression == null || binary_expression.Operator != BinaryOperator.ValueInequality)
					return false;

				var enumerator_reference = binary_expression.Left as VariableReferenceExpression;
				if (enumerator_reference == null || !IsEnumerator (enumerator_reference))
					return false;

				var null_literal = binary_expression.Right as LiteralExpression;
				if (null_literal == null || null_literal.Value != null)
					return false;

				state = State.IfBody;

				if (!VisitExpressionStatement (if_statement.Then.Statements [0]))
					return false;

				return true;
			}

			bool VisitWhileStatement (Statement node)
			{
				var while_statement = node as WhileStatement;
				if (while_statement == null || while_statement.Body.Statements.Count < 1)
					return false;

				state = State.WhileCondition;

				if (!VisitMethodInvocationExpression (while_statement.Condition))
					return false;

				state = State.WhileBody;

				if (!VisitExpressionStatement (while_statement.Body.Statements [0]))
					return false;

				this.while_body = new BlockStatement ();
				for (int i = 1; i < while_statement.Body.Statements.Count; i++) {
					this.while_body.Statements.Add (while_statement.Body.Statements [i]);
				}

				return true;
			}

			bool VisitMethodInvocationExpression (Expression node)
			{
				var invocation_expression = node as MethodInvocationExpression;
				if (invocation_expression == null)
					return false;

				var method_reference_expression = invocation_expression.Method as MethodReferenceExpression;
				if (method_reference_expression == null)
					return false;

				// todo : use a resolver for method calls
				switch (state) {
					case State.Begin:
						if (method_reference_expression.Method.Name != "GetEnumerator")
							return false;
						this.source = method_reference_expression.Target;
						break;

					case State.WhileCondition:
						if (!IsCallOnEnumerator (method_reference_expression, "MoveNext"))
							return false;
						break;

					case State.WhileBody:
						if (!IsCallOnEnumerator (method_reference_expression, "get_Current"))
							return false;
						break;

					case State.IfBody:
						if (!IsCallOnEnumerator (method_reference_expression, "Dispose"))
							return false;
						break;
				}

				return true;
			}

			bool VisitVariableReferenceExpression (Expression node)
			{
				var variable_reference_expression = node as VariableReferenceExpression;
				if (variable_reference_expression == null)
					return false;

				switch (state) {
					case State.Begin:
						this.enumerator = variable_reference_expression.Variable;
						break;

					case State.WhileBody:
						this.variable = variable_reference_expression.Variable as VariableDefinition;
						break;
				}

				return true;
			}

			bool IsCallOnEnumerator (MethodReferenceExpression method_reference_expression, string method_name)
			{
				if (method_reference_expression.Method.Name != method_name)
					return false;
				if (!IsEnumerator (method_reference_expression.Target))
					return false;
				return true;
			}

			bool IsEnumerator (Expression node)
			{
				var variable_reference = node as VariableReferenceExpression;
				if (variable_reference == null || variable_reference.Variable != this.enumerator)
					return false;
				return true;
			}

			static bool IsValidTryStatement (TryStatement try_statement)
			{
				return try_statement.CatchClauses.Count == 0 &&
				       try_statement.Try.Statements.Count == 1 &&
				       try_statement.Finally != null &&
				       try_statement.Finally.Statements.Count == 1;
			}
		}
	}
}