using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{
	/// <summary>
	/// Represents a function call expression.
	/// </summary>
	public class FunctionCallExpression : OperatorExpression
	{
		/// <summary>
		/// Creates a new instance of FunctionCallJSExpression.
		/// </summary>
		/// <param name="operator"> The binary operator to base this expression on. </param>
		public FunctionCallExpression (Operator @operator)
			: base (@operator)
		{
		}

		/// <summary>
		/// Gets an expression that evaluates to the function instance.
		/// </summary>
		public Expression Target {
			get { return GetOperand (0); }
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override PrimitiveType ResultType {
			get { return PrimitiveType.Any; }
		}
	}

}