using System;

namespace Jurassic.Compiler
{
	/// <summary>
	/// Represents a "new" expression.
	/// </summary>
	public sealed class NewExpression : OperatorExpression
	{
		/// <summary>
		/// Creates a new instance of NewExpression.
		/// </summary>
		/// <param name="operator"> The operator to base this expression on. </param>
		public NewExpression (Operator @operator)
			: base (@operator)
		{
		}

		/// <summary>
		/// Gets the precedence of the operator.
		/// </summary>
		public override int Precedence {
			get {
				// The expression "new String('').toString()" is parsed as
				// "new (String('').toString())" rather than "(new String('')).toString()".
				// There is no way to express this constraint properly using the standard operator
				// rules so we artificially boost the precedence of the operator if a function
				// call is encountered.  Note: GetRawOperand() is used instead of GetOperand(0)
				// because parentheses around the function call affect the result.
				return OperandCount == 1 && GetRawOperand (0) is FunctionCallExpression ?
                    int.MaxValue : Operator.New.Precedence;
			}
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override PrimitiveType ResultType {
			get { return PrimitiveType.Object; }
		}
	}

}