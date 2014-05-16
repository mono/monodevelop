using System;
using System.Collections.Generic;

namespace Jurassic.Compiler
{
    /// <summary>
    /// Represents an assignment expression (++, --, =, +=, -=, *=, /=, %=, &=, |=, ^=, &lt;&lt;=, &gt;&gt;=, &gt;&gt;&gt;=).
    /// </summary>
    public class AssignmentExpression : OperatorExpression
    {
        /// <summary>
        /// Creates a new instance of AssignmentExpression.
        /// </summary>
        /// <param name="operator"> The operator to base this expression on. </param>
        public AssignmentExpression(Operator @operator)
            : base(@operator)
        {
        }

        /// <summary>
        /// Creates a simple variable assignment expression.
        /// </summary>
        /// <param name="scope"> The scope the variable is defined within. </param>
        /// <param name="name"> The name of the variable to set. </param>
        /// <param name="value"> The value to set the variable to. </param>
        public AssignmentExpression(Scope scope, string name, Expression value)
            : base(Operator.Assignment)
        {
            this.Push(new NameExpression(scope, name));
            this.Push(value);
        }

        /// <summary>
        /// Gets the target of the assignment.
        /// </summary>
        public IReferenceExpression Target
        {
            get { return this.GetOperand(0) as IReferenceExpression; }
        }

        /// <summary>
        /// Gets the underlying base operator for the given compound operator.
        /// </summary>
        /// <param name="compoundOperatorType"> The type of compound operator. </param>
        /// <returns> The underlying base operator, or <c>null</c> if the type is not a compound
        /// operator. </returns>
        private static Operator GetCompoundBaseOperator(OperatorType compoundOperatorType)
        {
            switch (compoundOperatorType)
            {
                case OperatorType.CompoundAdd:
                    return Operator.Add;
                case OperatorType.CompoundBitwiseAnd:
                    return Operator.BitwiseAnd;
                case OperatorType.CompoundBitwiseOr:
                    return Operator.BitwiseOr;
                case OperatorType.CompoundBitwiseXor:
                    return Operator.BitwiseXor;
                case OperatorType.CompoundDivide:
                    return Operator.Divide;
                case OperatorType.CompoundLeftShift:
                    return Operator.LeftShift;
                case OperatorType.CompoundModulo:
                    return Operator.Modulo;
                case OperatorType.CompoundMultiply:
                    return Operator.Multiply;
                case OperatorType.CompoundSignedRightShift:
                    return Operator.SignedRightShift;
                case OperatorType.CompoundSubtract:
                    return Operator.Subtract;
                case OperatorType.CompoundUnsignedRightShift:
                    return Operator.UnsignedRightShift;
            }
            return null;
        }

        /// <summary>
        /// Gets the type that results from evaluating this expression.
        /// </summary>
        public override PrimitiveType ResultType
        {
            get
            {
                var type = this.OperatorType;
                if (type == OperatorType.PostIncrement ||
                    type == OperatorType.PostDecrement ||
                    type == OperatorType.PreIncrement ||
                    type == OperatorType.PreDecrement)
                    return PrimitiveType.Number;
                if (type == OperatorType.Assignment)
                    return this.GetOperand(1).ResultType;
                var compoundOperator = new BinaryExpression(GetCompoundBaseOperator(type), this.GetOperand(0), this.GetOperand(1));
                return compoundOperator.ResultType;
            }
        }
    }

}