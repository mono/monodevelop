using System;

namespace Jurassic.Compiler
{
	/// <summary>
	/// Represents a unary operator expression.
	/// </summary>
	public sealed class UnaryExpression : OperatorExpression
	{
		/// <summary>
		/// Creates a new instance of UnaryExpression.
		/// </summary>
		/// <param name="operator"> The unary operator to base this expression on. </param>
		public UnaryExpression (Operator @operator)
			: base (@operator)
		{
		}

		/// <summary>
		/// Gets the expression on the left or right side of the unary operator.
		/// </summary>
		public Expression Operand {
			get { return GetOperand (0); }
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override PrimitiveType ResultType {
			get {
				switch (OperatorType) {
				case OperatorType.Plus:
				case OperatorType.Minus:
					return PrimitiveType.Number;

				case OperatorType.BitwiseNot:
					return PrimitiveType.Int32;

				case OperatorType.LogicalNot:
					return PrimitiveType.Bool;

				case OperatorType.Void:
					return PrimitiveType.Undefined;

				case OperatorType.Typeof:
					return PrimitiveType.String;

				case OperatorType.Delete:
					return PrimitiveType.Bool;

				default:
					throw new NotImplementedException (string.Format ("Unsupported operator {0}", OperatorType));
				}
			}
		}
	}
}