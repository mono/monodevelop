using System;
using System.Collections.Generic;

using Cecil.Decompiler.Ast;

namespace Cecil.Decompiler.Steps {

	class OperatorStep : BaseCodeTransformer, IDecompilationStep {

		public static readonly IDecompilationStep Instance = new OperatorStep();

		static readonly Dictionary<string, BinaryOperator> binary_operators
			= new Dictionary<string, BinaryOperator> () {

			{ "op_Equality", BinaryOperator.ValueEquality },
			{ "op_Inequality", BinaryOperator.ValueInequality },
			{ "op_GreaterThan", BinaryOperator.GreaterThan },
			{ "op_GreaterThanOrEqual", BinaryOperator.GreaterThanOrEqual },
			{ "op_LessThan", BinaryOperator.LessThan },
			{ "op_LessThanOrEqual", BinaryOperator.LessThanOrEqual },
			{ "op_Addition", BinaryOperator.Add },
			{ "op_Subtraction", BinaryOperator.Subtract },
			{ "op_Division", BinaryOperator.Divide },
			{ "op_Multiply", BinaryOperator.Multiply },
			{ "op_Modulus", BinaryOperator.Modulo },
			{ "op_BitwiseAnd", BinaryOperator.BitwiseAnd },
			{ "op_BitwiseOr", BinaryOperator.BitwiseOr },
			{ "op_ExclusiveOr", BinaryOperator.BitwiseXor },
			{ "op_RightShift", BinaryOperator.RightShift },
			{ "op_LeftShift", BinaryOperator.LeftShift },
		};

		static readonly Dictionary<string, UnaryOperator> unary_operators
			= new Dictionary<string, UnaryOperator> () {

			{ "op_UnaryNegation", UnaryOperator.Negate },
			{ "op_LogicalNot", UnaryOperator.LogicalNot },
			{ "op_OnesComplement", UnaryOperator.BitwiseNot },
			{ "op_Decrement", UnaryOperator.PostDecrement },
			{ "op_Increment", UnaryOperator.PostIncrement },
		};

		public override ICodeNode VisitMethodInvocationExpression (MethodInvocationExpression node)
		{
			var method_reference = node.Method as MethodReferenceExpression;
			if (method_reference == null)
				goto skip;

			var method = method_reference.Method;

			BinaryOperator binary_operator;
			if (binary_operators.TryGetValue (method.Name, out binary_operator))
				return BuildBinaryExpression (binary_operator, node.Arguments [0], node.Arguments [1]);

			UnaryOperator unary_operator;
			if (unary_operators.TryGetValue (method.Name, out unary_operator))
				return BuildUnaryExpression (unary_operator, node.Arguments [0]);

		skip:
			return base.VisitMethodInvocationExpression (node);
		}

		ICodeNode BuildUnaryExpression (UnaryOperator @operator, Expression expression)
		{
			return new UnaryExpression (@operator, (Expression) Visit (expression));
		}

		ICodeNode BuildBinaryExpression (BinaryOperator @operator, Expression left, Expression right)
		{
			return new BinaryExpression (@operator,
				(Expression) Visit (left),
				(Expression) Visit (right));
		}

		public BlockStatement Process (DecompilationContext context, BlockStatement body)
		{
			return (BlockStatement) VisitBlockStatement (body);
		}
	}
}