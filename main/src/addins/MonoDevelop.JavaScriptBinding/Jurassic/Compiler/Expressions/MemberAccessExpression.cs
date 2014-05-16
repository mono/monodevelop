using System;

namespace Jurassic.Compiler
{
    
	/// <summary>
	/// Represents a variable or member access.
	/// </summary>
	public sealed class MemberAccessExpression : OperatorExpression, IReferenceExpression
	{
		/// <summary>
		/// Creates a new instance of MemberAccessExpression.
		/// </summary>
		/// <param name="operator"> The operator to base this expression on. </param>
		public MemberAccessExpression (Operator @operator)
			: base (@operator)
		{
		}

		/// <summary>
		/// Gets an expression that evaluates to the object that is being accessed or modified.
		/// </summary>
		public Expression Base {
			get { return GetOperand (0); }
		}

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override PrimitiveType ResultType {
			get { return PrimitiveType.Any; }
		}

		/// <summary>
		/// Gets the static type of the reference.
		/// </summary>
		public PrimitiveType Type {
			get { return PrimitiveType.Any; }
		}

		/// <summary>
		/// Converts the expression to a string.
		/// </summary>
		/// <returns> A string representing this expression. </returns>
		public override string ToString ()
		{
			if (OperatorType == OperatorType.MemberAccess)
				return string.Format ("{0}.{1}", GetRawOperand (0), OperandCount >= 2 ? GetRawOperand (1).ToString () : "?");
			return base.ToString ();
		}
	}
}