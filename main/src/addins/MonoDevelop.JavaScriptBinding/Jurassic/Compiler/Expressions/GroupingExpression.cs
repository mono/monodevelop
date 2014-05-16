using System;

namespace Jurassic.Compiler
{
    /// <summary>
    /// Represents a grouping expression.
    /// </summary>
    public sealed class GroupingExpression : OperatorExpression
    {
        /// <summary>
        /// Creates a new instance of GroupingJSExpression.
        /// </summary>
        /// <param name="operator"> The operator to base this expression on. </param>
        public GroupingExpression(Operator @operator)
            : base(@operator)
        {
        }

        /// <summary>
        /// Gets the expression inside the grouping operator.
        /// </summary>
        public Expression Operand
        {
            get { return this.GetOperand(0); }
        }

        /// <summary>
        /// Gets the type that results from evaluating this expression.
        /// </summary>
        public override PrimitiveType ResultType
        {
            get { return this.Operand.ResultType; }
        }
    }

}