using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{

    /// <summary>
    /// Represents a javascript for-in statement.
    /// </summary>
    public class ForInStatement : Statement
    {
        /// <summary>
        /// Creates a new ForInStatement instance.
        /// </summary>
        /// <param name="labels"> The labels that are associated with this statement. </param>
        public ForInStatement(IList<string> labels)
            : base(labels)
        {
        }

        /// <summary>
        /// Gets or sets a reference to mutate on each iteration of the loop.
        /// </summary>
        public IReferenceExpression Variable
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the portion of source code associated with the variable.
        /// </summary>
        public SourceCodeSpan VariableSourceSpan
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an expression that evaluates to the object to enumerate.
        /// </summary>
        public Expression TargetObject
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the portion of source code associated with the target object.
        /// </summary>
        public SourceCodeSpan TargetObjectSourceSpan
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the loop body.
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
                yield return this.TargetObject;

                // Fake a string assignment to the target variable so it gets the correct type.
                var fakeAssignment = new AssignmentExpression(Operator.Assignment);
                fakeAssignment.Push((Expression)this.Variable);
                fakeAssignment.Push(new LiteralExpression(""));
                yield return fakeAssignment;

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
            result.AppendFormat("for ({0} in {1})",
                this.Variable.ToString(),
                this.TargetObject.ToString());
            result.AppendLine();
            result.Append(this.Body.ToString(indentLevel + 1));
            return result.ToString();
        }
    }

}