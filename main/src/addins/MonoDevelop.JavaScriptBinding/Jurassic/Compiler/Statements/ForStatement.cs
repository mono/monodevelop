using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents a javascript for statement (for-in is a separate statement).
	/// </summary>
	public class ForStatement : LoopStatement
	{
		/// <summary>
		/// Creates a new ForStatement instance.
		/// </summary>
		/// <param name="labels"> The labels that are associated with this statement. </param>
		public ForStatement (IList<string> labels)
			: base (labels)
		{
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
			result.AppendFormat ("for ({0} {1} {2})",
				InitStatement == null ? ";" : InitStatement.ToString (0),
				ConditionStatement == null ? ";" : ConditionStatement.ToString (),
				IncrementStatement == null ? string.Empty : Increment.ToString ());
			result.AppendLine ();
			result.Append (Body.ToString (indentLevel + 1));
			return result.ToString ();
		}
	}

}