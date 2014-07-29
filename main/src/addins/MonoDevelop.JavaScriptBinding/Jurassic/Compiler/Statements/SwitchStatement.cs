using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents a javascript switch statement.
	/// </summary>
	public class SwitchStatement : Statement
	{
		/// <summary>
		/// Creates a new SwitchStatement instance.
		/// </summary>
		/// <param name="labels"> The labels that are associated with this statement. </param>
		public SwitchStatement (IList<string> labels)
			: base (labels)
		{
			CaseClauses = new List<SwitchCase> (5);
		}

		/// <summary>
		/// Gets or sets an expression that represents the value to switch on.
		/// </summary>
		public Expression Value {
			get;
			set;
		}

		/// <summary>
		/// Gets a list of case clauses (including the default clause).
		/// </summary>
		public IList<SwitchCase> CaseClauses {
			get;
			private set;
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<JSAstNode> ChildNodes {
			get {
				yield return Value;
				foreach (var clause in CaseClauses) {
					if (clause.Value != null)
						yield return clause.Value;
					foreach (var statement in clause.BodyStatements)
						yield return statement;
				}
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
			result.Append ("switch (");
			result.Append (Value);
			result.AppendLine (")");
			result.Append (new string ('\t', indentLevel));
			result.AppendLine ("{");
			indentLevel++;
			foreach (var caseClause in CaseClauses) {
				result.Append (new string ('\t', indentLevel));
				if (caseClause.Value != null) {
					result.Append ("case ");
					result.Append (caseClause.Value);
					result.AppendLine (":");
				} else {
					result.AppendLine ("default:");
				}
				foreach (var statement in caseClause.BodyStatements)
					result.AppendLine (statement.ToString (indentLevel + 1));
			}
			indentLevel--;
			result.Append (new string ('\t', indentLevel));
			result.Append ("}");
			return result.ToString ();
		}
	}

	/// <summary>
	/// Represents a single case statement inside a switch statement.
	/// </summary>
	public class SwitchCase
	{
		public SwitchCase ()
		{
			this.BodyStatements = new List<Statement> ();
		}

		/// <summary>
		/// Gets or sets an expression that must match the switch value for the case to execute.
		/// A value of <c>null</c> indicates this is the default clause.
		/// </summary>
		public Expression Value {
			get;
			set;
		}

		/// <summary>
		/// Gets a list of the statement(s) in the body of the case statement.
		/// </summary>
		public IList<Statement> BodyStatements {
			get;
			private set;
		}
	}
}