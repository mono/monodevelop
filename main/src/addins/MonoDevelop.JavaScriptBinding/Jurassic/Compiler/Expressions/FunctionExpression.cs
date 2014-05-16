using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{
	/// <summary>
	/// Represents a function expression.
	/// </summary>
	public sealed class FunctionExpression : Expression
	{
		FunctionMethodGenerator context;

		/// <summary>
		/// Creates a new instance of FunctionExpression.
		/// </summary>
		/// <param name="functionContext"> The function context to base this expression on. </param>
		public FunctionExpression (FunctionMethodGenerator functionContext, SourceCodeSpan sourceSpan)
		{
			if (functionContext == null)
				throw new ArgumentNullException ("functionContext");
			context = functionContext;
			SourceSpan = sourceSpan;
		}

		/// <summary>
		/// Gets the name of the function.
		/// </summary>
		public string FunctionName {
			get { return context.Name; }
		}

		/// <summary>
		/// Gets a list of argument names.
		/// </summary>
		public IList<string> ArgumentNames {
			get { return context.ArgumentNames; }
		}

		/// <summary>
		/// Gets the source code for the body of the function.
		/// </summary>
		public string BodyText {
			get { return context.BodyText; }
		}

		public Statement BodyRoot {
			get {
				return context.BodyRoot;
			}
		}

		public SourceCodeSpan SourceSpan { get; set; }

		/// <summary>
		/// Gets the type that results from evaluating this expression.
		/// </summary>
		public override PrimitiveType ResultType {
			get { return PrimitiveType.Object; }
		}

		/// <summary>
		/// Converts the expression to a string.
		/// </summary>
		/// <returns> A string representing this expression. </returns>
		public override string ToString ()
		{
			return string.Format ("function {0}({1}) {{\n{2}\n}}", FunctionName, StringHelpers.Join (", ", ArgumentNames), BodyText);
		}
	}

}