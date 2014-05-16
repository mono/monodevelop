using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents a literal expression.
	/// </summary>
	public sealed class LiteralExpression : Expression
	{
		/// <summary>
		/// Creates a new instance of LiteralJSExpression.
		/// </summary>
		/// <param name="value"> The literal value. </param>
		public LiteralExpression (object value)
		{
			this.Value = value;
		}

		/// <summary>
		/// Gets the literal value.
		/// </summary>
		public object Value { get; private set; }
	}
}