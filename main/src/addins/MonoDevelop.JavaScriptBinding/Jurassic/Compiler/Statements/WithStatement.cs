using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

    /// <summary>
    /// Represents a javascript with statement.
    /// </summary>
    public class WithStatement : Statement
    {
        /// <summary>
        /// Creates a new WithStatement instance.
        /// </summary>
        /// <param name="labels"> The labels that are associated with this statement. </param>
        public WithStatement(IList<string> labels)
            : base(labels)
        {
        }

        /// <summary>
        /// Gets or sets the object scope inside the with statement.
        /// </summary>
        public ObjectScope Scope
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the body of the with statement.
        /// </summary>
        public Statement Body
        {
            get;
            set;
        }

        /// <summary>
        /// Gets an enumerable list of child nodes in the abstract syntax tree.
        /// </summary>
        public override IEnumerable<JSAstNode> ChildNodes
        {
            get
            {
                yield return this.Scope.ScopeObjectExpression;
                yield return this.Body;
            }
        }

        /// <summary>
        /// Converts the statement to a string.
        /// </summary>
        /// <param name="indentLevel"> The number of tabs to include before the statement. </param>
        /// <returns> A string representing this statement. </returns>
        public override string ToString(int indentLevel)
        {
            var result = new System.Text.StringBuilder();
            result.Append(new string('\t', indentLevel));
            result.Append("with (");
            result.Append(this.Scope.ScopeObjectExpression);
            result.AppendLine(")");
            result.Append(this.Body.ToString(indentLevel + 1));
            return result.ToString();
        }
    }

}