using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents a javascript block statement.
	/// </summary>
	public class BlockStatement : Statement
	{
		private List<Statement> statements = new List<Statement> ();

		/// <summary>
		/// Creates a new BlockStatement instance.
		/// </summary>
		/// <param name="labels"> The labels that are associated with this statement. </param>
		public BlockStatement (IList<string> labels)
			: base (labels)
		{
		}

		/// <summary>
		/// Gets a list of the statements in the block.
		/// </summary>
		public IList<Statement> Statements {
			get { return statements; }
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<JSAstNode> ChildNodes {
			get {
				foreach (var statement in Statements)
					yield return statement;
			}
		}

		/// <summary>
		/// Converts the statement to a string.
		/// </summary>
		/// <param name="indentLevel"> The number of tabs to include before the statement. </param>
		/// <returns> A string representing this statement. </returns>
		public override string ToString (int indentLevel)
		{
			indentLevel = Math.Max (indentLevel - 1, 0);
			var result = new System.Text.StringBuilder ();
			result.Append (new string ('\t', indentLevel));
			result.AppendLine ("{");
			foreach (var statement in Statements)
				result.AppendLine (statement.ToString (indentLevel + 1));
			result.Append (new string ('\t', indentLevel));
			result.Append ("}");
			return result.ToString ();
		}
	}

}