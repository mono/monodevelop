using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents an if statement.
	/// </summary>
	public class IfStatement : Statement
	{
		/// <summary>
		/// Creates a new IfStatement instance.
		/// </summary>
		/// <param name="labels"> The labels that are associated with this statement. </param>
		public IfStatement (IList<string> labels)
			: base (labels)
		{
		}

		/// <summary>
		/// Gets or sets the condition that determines which path the code should proceed.
		/// </summary>
		public Expression Condition {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the statement that is executed if the condition is true.
		/// </summary>
		public Statement IfClause {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the statement that is executed if the condition is false.
		/// </summary>
		public Statement ElseClause {
			get;
			set;
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<JSAstNode> ChildNodes {
			get {
				yield return Condition;
				yield return IfClause;
				if (ElseClause != null)
					yield return ElseClause;
			}
		}

		/// <summary>
		/// Converts the statement to a string.
		/// </summary>
		/// <param name="indentLevel"> The number of tabs to include before the statement. </param>
		/// <returns> A string representing this statement. </returns>
		public override string ToString (int indentLevel)
		{
			var result = new System.Text.StringBuilder ();
			result.Append (new string ('\t', indentLevel));
			result.Append ("if (");
			result.Append (Condition.ToString ());
			result.AppendLine (")");
			result.AppendLine (IfClause.ToString (indentLevel + 1));
			if (ElseClause != null) {
				result.Append (new string ('\t', indentLevel));
				result.AppendLine ("else");
				result.AppendLine (ElseClause.ToString (indentLevel + 1));
			}
			return result.ToString ();
		}
	}

}