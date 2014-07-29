using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

	/// <summary>
	/// Represents a javascript var statement.
	/// </summary>
	public class VarStatement : Statement
	{
		private List<VariableDeclaration> declarations;

		/// <summary>
		/// Creates a new VarStatement instance.
		/// </summary>
		/// <param name="labels"> The labels that are associated with this statement. </param>
		/// <param name="scope"> The scope the variables are defined within. </param>
		public VarStatement (IList<string> labels, Scope scope)
			: base (labels)
		{
			if (scope == null)
				throw new ArgumentNullException ("scope");
			Scope = scope;
			declarations = new List<VariableDeclaration> (1);
		}

		/// <summary>
		/// Gets the scope the variables are defined within.
		/// </summary>
		public Scope Scope {
			get;
			private set;
		}

		/// <summary>
		/// Gets a list of variable declarations.
		/// </summary>
		public IList<VariableDeclaration> Declarations {
			get { return declarations; }
		}

		/// <summary>
		/// Gets an enumerable list of child nodes in the abstract syntax tree.
		/// </summary>
		public override IEnumerable<JSAstNode> ChildNodes {
			get {
				foreach (var declaration in Declarations)
					if (declaration.InitExpression != null)
						yield return new AssignmentExpression (Scope, declaration.VariableName, declaration.InitExpression);
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
			result.Append ("var ");
			bool first = true;
			foreach (var declaration in Declarations) {
				if (!first)
					result.Append (", ");
				first = false;
				result.Append (declaration.VariableName);
				if (declaration.InitExpression != null) {
					result.Append (" = ");
					result.Append (declaration.InitExpression);
				}
			}
			result.Append (";");
			return result.ToString ();
		}
	}

	/// <summary>
	/// Represents a variable declaration.
	/// </summary>
	public class VariableDeclaration
	{
		/// <summary>
		/// Gets or sets the name of the variable that is being declared.
		/// </summary>
		public string VariableName { get; set; }

		/// <summary>
		/// Gets or sets the initial value of the variable.  Can be <c>null</c>.
		/// </summary>
		public Expression InitExpression { get; set; }

		/// <summary>
		/// Gets or sets the portion of source code associated with the declaration.
		/// </summary>
		public SourceCodeSpan SourceSpan { get; set; }
	}

}