using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

    /// <summary>
    /// Represents a return statement.
    /// </summary>
    public class ReturnStatement : Statement
    {
        /// <summary>
        /// Creates a new ReturnStatement instance.
        /// </summary>
        /// <param name="labels"> The labels that are associated with this statement. </param>
        public ReturnStatement(IList<string> labels)
            : base(labels)
        {
        }

        /// <summary>
        /// Gets or sets the expression to return.  Can be <c>null</c> to return "undefined".
        /// </summary>
        public Expression Value
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
                if (this.Value != null)
                    yield return this.Value;
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
            result.Append("return");
            if (this.Value != null)
            {
                result.Append(" ");
                result.Append(this.Value);
            }
            result.Append(";");
            return result.ToString();
        }
    }

}