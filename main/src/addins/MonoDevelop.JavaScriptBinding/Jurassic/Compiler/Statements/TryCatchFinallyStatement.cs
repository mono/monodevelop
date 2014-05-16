using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents a try-catch-finally statement.
	/// </summary>
	public class TryCatchFinallyStatement : Statement
	{
		/// <summary>
		/// Creates a new TryCatchFinallyStatement instance.
		/// </summary>
		public TryCatchFinallyStatement (IList<string> labels)
			: base (labels)
		{
		}

		/// <summary>
		/// Gets or sets the statement(s) inside the try block.
		/// </summary>
		public BlockStatement TryBlock {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the statement(s) inside the catch block.  Can be <c>null</c>.
		/// </summary>
		public BlockStatement CatchBlock {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the scope of the variable to receive the exception.  Can be <c>null</c> but
		/// only if CatchStatement is also <c>null</c>.
		/// </summary>
		public Scope CatchScope {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the name of the variable to receive the exception.  Can be <c>null</c> but
		/// only if CatchStatement is also <c>null</c>.
		/// </summary>
		public string CatchVariableName {
			get;
			set;
		}

		/// <summary>
		/// Gets or sets the statement(s) inside the finally block.  Can be <c>null</c>.
		/// </summary>
		public BlockStatement FinallyBlock {
			get;
			set;
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<JSAstNode> ChildNodes {
			get {
				yield return TryBlock;
				if (CatchBlock != null)
					yield return CatchBlock;
				if (FinallyBlock != null)
					yield return FinallyBlock;
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
			result.AppendLine ("try");
			result.AppendLine (TryBlock.ToString (indentLevel + 1));
			if (CatchBlock != null) {
				result.Append (new string ('\t', indentLevel));
				result.Append ("catch (");
				result.Append (CatchVariableName);
				result.AppendLine (")");
				result.AppendLine (CatchBlock.ToString (indentLevel + 1));
			}
			if (FinallyBlock != null) {
				result.Append (new string ('\t', indentLevel));
				result.AppendLine ("finally");
				result.AppendLine (FinallyBlock.ToString (indentLevel + 1));
			}
			return result.ToString ();
		}
	}

}