using System;
using System.Collections.Generic;
using System.Text;

namespace XmlDocIdLib
{
    #region OperatorType enumeration
    enum OperatorType
    {
        None,
        op_Implicit,
        op_Explicit,

        // overloadable unary operators
        op_Decrement,                   // --
        op_Increment,                   // ++
        op_UnaryNegation,               // -
        op_UnaryPlus,                   // +
        op_LogicalNot,                  // !
        op_True,                        // true
        op_False,                       // false
        op_OnesComplement,              // ~
        op_Like,                        // Like (Visual Basic)


        // overloadable binary operators
        op_Addition,                    // +
        op_Subtraction,                 // -
        op_Division,                    // /
        op_Multiply,                    // *
        op_Modulus,                     // %
        op_BitwiseAnd,                  // &
        op_ExclusiveOr,                 // ^
        op_LeftShift,                   // <<
        op_RightShift,                  // >>
        op_BitwiseOr,                   // |

        // overloadable comparision operators
        op_Equality,                    // ==
        op_Inequality,                  // != 
        op_LessThanOrEqual,             // <=
        op_LessThan,                    // <
        op_GreaterThanOrEqual,          // >=
        op_GreaterThan,                 // >

        // not overloadable operators
        op_AddressOf,                       // &
        op_PointerDereference,              // *
        op_LogicalAnd,                      // &&
        op_LogicalOr,                       // ||
        op_Assign,                          // Not defined (= is not the same)
        op_SignedRightShift,                // Not defined
        op_UnsignedRightShift,              // Not defined
        op_UnsignedRightShiftAssignment,    // Not defined
        op_MemberSelection,                 // ->
        op_RightShiftAssignment,            // >>=
        op_MultiplicationAssignment,        // *=
        op_PointerToMemberSelection,        // ->*
        op_SubtractionAssignment,           // -=
        op_ExclusiveOrAssignment,           // ^=
        op_LeftShiftAssignment,             // <<=
        op_ModulusAssignment,               // %=
        op_AdditionAssignment,              // +=
        op_BitwiseAndAssignment,            // &=
        op_BitwiseOrAssignment,             // |=
        op_Comma,                           // ,
        op_DivisionAssignment               // /=
    }
    #endregion
}
