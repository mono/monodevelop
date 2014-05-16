using System;

namespace Jurassic.Compiler
{
    /// <summary>
    /// Represents a ternary operator expression.
    /// </summary>
    public sealed class TernaryExpression : OperatorExpression
    {
        /// <summary>
        /// Creates a new instance of TernaryExpression.
        /// </summary>
        /// <param name="operator"> The ternary operator to base this expression on. </param>
        public TernaryExpression(Operator @operator)
            : base(@operator)
        {
        }

        /// <summary>
        /// Gets the type that results from evaluating this expression.
        /// </summary>
        public override PrimitiveType ResultType
        {
            get
            {
                // The result is either the type of the second operand or the third operand.
                var a = this.GetOperand(1).ResultType;
                var b = this.GetOperand(2).ResultType;
                if (a == b)
                    return a;
                if (PrimitiveTypeUtilities.IsNumeric(a) == true && PrimitiveTypeUtilities.IsNumeric(b) == true)
                    return PrimitiveType.Number;
                return PrimitiveType.Any;
            }
        }

    }

}