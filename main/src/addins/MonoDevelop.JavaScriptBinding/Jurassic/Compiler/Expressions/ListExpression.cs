using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{
	/// <summary>
	/// Represents a comma-delimited list.
	/// </summary>
	public class ListExpression : OperatorExpression
	{
		/// <summary>
		/// Creates a new instance of ListExpression.
		/// </summary>
		/// <param name="operator"> The operator to base this expression on. </param>
		public ListExpression (Operator @operator)
			: base (@operator)
		{
		}

		/// <summary>
		/// Gets an array of expressions, one for each item in the list.
		/// </summary>
		public IList<Expression> Items {
			get {
				var result = new List<Expression> ();
				Expression leftHandSide = this;
				while (leftHandSide is ListExpression) {
					result.Add (((ListExpression)leftHandSide).GetRawOperand (1));
					leftHandSide = ((ListExpression)leftHandSide).GetRawOperand (0);
				}
				result.Add (leftHandSide);
				result.Reverse ();
				return result;
			}
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override PrimitiveType ResultType {
			get {
				return GetOperand (1).ResultType;
			}
		}

		/// <summary>
		/// Converts the expression to a string.
		/// </summary>
		/// <returns> A string representing this expression. </returns>
		public override string ToString ()
		{
			return string.Format ("{0}, {1}", GetOperand (0), GetOperand (1));
		}
	}

}