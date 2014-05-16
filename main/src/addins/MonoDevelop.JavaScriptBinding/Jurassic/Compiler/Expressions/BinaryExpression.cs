using System;
using System.Diagnostics;

namespace Jurassic.Compiler
{
    /// <summary>
    /// Represents a binary operator expression.
    /// </summary>
    public class BinaryExpression : OperatorExpression
    {
        /// <summary>
        /// Creates a new instance of BinaryExpression.
        /// </summary>
        /// <param name="operator"> The binary operator to base this expression on. </param>
        public BinaryExpression(Operator @operator)
            : base(@operator)
        {
        }

        /// <summary>
        /// Creates a new instance of BinaryJSExpression.
        /// </summary>
        /// <param name="operator"> The binary operator to base this expression on. </param>
        /// <param name="left"> The operand on the left side of the operator. </param>
        /// <param name="right"> The operand on the right side of the operator. </param>
        public BinaryExpression(Operator @operator, Expression left, Expression right)
            : base(@operator)
        {
            this.Push(left);
            this.Push(right);
        }

        /// <summary>
        /// Gets the expression on the left side of the operator.
        /// </summary>
        public Expression Left
        {
            get { return this.GetOperand(0); }
        }

        /// <summary>
        /// Gets the expression on the right side of the operator.
        /// </summary>
        public Expression Right
        {
            get { return this.GetOperand(1); }
        }

        /// <summary>
        /// Gets the type that results from evaluating this expression.
        /// </summary>
        public override PrimitiveType ResultType
        {
            get
            {
                var type = this.OperatorType;
                switch (this.OperatorType)
                {
                    // Add
                    case OperatorType.Add:
                        {
                            var lhs = this.Left.ResultType;
                            var rhs = this.Right.ResultType;
                            if (lhs == PrimitiveType.String || rhs == PrimitiveType.String)
                                return PrimitiveType.ConcatenatedString;
                            if (lhs == PrimitiveType.ConcatenatedString || rhs == PrimitiveType.ConcatenatedString)
                                return PrimitiveType.ConcatenatedString;
                            if (lhs == PrimitiveType.Any || lhs == PrimitiveType.Object ||
                                rhs == PrimitiveType.Any || rhs == PrimitiveType.Object)
                                return PrimitiveType.Any;
                            return PrimitiveType.Number;
                        }

                    // Arithmetic operations.
                    case OperatorType.Subtract:
                    case OperatorType.Multiply:
                    case OperatorType.Divide:
                    case OperatorType.Modulo:
                        return PrimitiveType.Number;

                    // Bitwise operations.
                    case OperatorType.BitwiseAnd:
                    case OperatorType.BitwiseOr:
                    case OperatorType.BitwiseXor:
                    case OperatorType.LeftShift:
                    case OperatorType.SignedRightShift:
                        return PrimitiveType.Int32;
                    case OperatorType.UnsignedRightShift:
                        return PrimitiveType.Number;

                    // Relational operations.
                    case OperatorType.LessThan:
                    case OperatorType.LessThanOrEqual:
                    case OperatorType.GreaterThan:
                    case OperatorType.GreaterThanOrEqual:
                        return PrimitiveType.Bool;

                    // Equality operations.
                    case OperatorType.Equal:
                    case OperatorType.StrictlyEqual:
                    case OperatorType.NotEqual:
                    case OperatorType.StrictlyNotEqual:
                        return PrimitiveType.Bool;
                    
                    // Logical operations.
                    case OperatorType.LogicalAnd:
                    case OperatorType.LogicalOr:
                        {
                            // The result is either the left-hand side or the right-hand side.
                            var lhs = this.Left.ResultType;
                            var rhs = this.Right.ResultType;
                            if (lhs == rhs)
                                return lhs;
                            if (PrimitiveTypeUtilities.IsNumeric(lhs) == true && PrimitiveTypeUtilities.IsNumeric(rhs) == true)
                                return PrimitiveType.Number;
                            return PrimitiveType.Any;
                        }

                    // Misc
                    case OperatorType.InstanceOf:
                    case OperatorType.In:
                        return PrimitiveType.Bool;
                }
                throw new NotImplementedException();
            }
        }
    }
}