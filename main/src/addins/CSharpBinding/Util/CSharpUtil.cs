// 
// CSharpUtil.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace ICSharpCode.NRefactory6.CSharp
{
	static class CSharpUtil
	{
		/// <summary>
		/// Inverts a boolean condition. Note: The condition object can be frozen (from AST) it's cloned internally.
		/// </summary>
		/// <param name="condition">The condition to invert.</param>
		public static ExpressionSyntax InvertCondition (ExpressionSyntax condition)
		{
			return InvertConditionInternal (condition);
		}

		static ExpressionSyntax InvertConditionInternal (ExpressionSyntax condition)
		{
			if (condition is ParenthesizedExpressionSyntax) {
				return SyntaxFactory.ParenthesizedExpression (InvertCondition (((ParenthesizedExpressionSyntax)condition).Expression));
			}

			if (condition is PrefixUnaryExpressionSyntax) {
				var uOp = (PrefixUnaryExpressionSyntax)condition;
				if (uOp.IsKind (SyntaxKind.LogicalNotExpression)) {
					if (!(uOp.Parent is ExpressionSyntax))
						return uOp.Operand.SkipParens ();
					return uOp.Operand;
				}
				return SyntaxFactory.PrefixUnaryExpression (SyntaxKind.LogicalNotExpression, uOp);
			}

			if (condition is BinaryExpressionSyntax) {
				var bOp = (BinaryExpressionSyntax)condition;

				if (bOp.IsKind (SyntaxKind.LogicalAndExpression) || bOp.IsKind (SyntaxKind.LogicalOrExpression))
					return SyntaxFactory.BinaryExpression (NegateConditionOperator (bOp.Kind ()), InvertCondition (bOp.Left), InvertCondition (bOp.Right));

				if (bOp.IsKind (SyntaxKind.EqualsExpression) ||
					bOp.IsKind (SyntaxKind.NotEqualsExpression) ||
					bOp.IsKind (SyntaxKind.GreaterThanExpression) ||
					bOp.IsKind (SyntaxKind.GreaterThanOrEqualExpression) ||
					bOp.IsKind (SyntaxKind.LessThanExpression) ||
					bOp.IsKind (SyntaxKind.LessThanOrEqualExpression))
					return SyntaxFactory.BinaryExpression (NegateRelationalOperator (bOp.Kind ()), bOp.Left, bOp.Right);

				return SyntaxFactory.PrefixUnaryExpression (SyntaxKind.LogicalNotExpression, SyntaxFactory.ParenthesizedExpression (condition));
			}

			if (condition is ConditionalExpressionSyntax) {
				var cEx = condition as ConditionalExpressionSyntax;
				return cEx.WithCondition (InvertCondition (cEx.Condition));
			}

			if (condition is LiteralExpressionSyntax) {
				if (condition.Kind () == SyntaxKind.TrueLiteralExpression)
					return SyntaxFactory.LiteralExpression (SyntaxKind.FalseLiteralExpression);
				if (condition.Kind () == SyntaxKind.FalseLiteralExpression)
					return SyntaxFactory.LiteralExpression (SyntaxKind.TrueLiteralExpression);
			}

			return SyntaxFactory.PrefixUnaryExpression (SyntaxKind.LogicalNotExpression, AddParensForUnaryExpressionIfRequired (condition));
		}

		/// <summary>
		/// When negating an expression this is required, otherwise you would end up with
		/// a or b -> !a or b
		/// </summary>
		public static ExpressionSyntax AddParensForUnaryExpressionIfRequired (ExpressionSyntax expression)
		{
			if ((expression is BinaryExpressionSyntax) ||
				(expression is AssignmentExpressionSyntax) ||
				(expression is CastExpressionSyntax) ||
				(expression is ParenthesizedLambdaExpressionSyntax) ||
				(expression is SimpleLambdaExpressionSyntax) ||
				(expression is ConditionalExpressionSyntax)) {
				return SyntaxFactory.ParenthesizedExpression (expression);
			}

			return expression;
		}

		/// <summary>
		/// Get negation of the specified relational operator
		/// </summary>
		/// <returns>
		/// negation of the specified relational operator, or BinaryOperatorType.Any if it's not a relational operator
		/// </returns>
		public static SyntaxKind NegateRelationalOperator (SyntaxKind op)
		{
			switch (op) {
			case SyntaxKind.EqualsExpression:
				return SyntaxKind.NotEqualsExpression;
			case SyntaxKind.NotEqualsExpression:
				return SyntaxKind.EqualsExpression;
			case SyntaxKind.GreaterThanExpression:
				return SyntaxKind.LessThanOrEqualExpression;
			case SyntaxKind.GreaterThanOrEqualExpression:
				return SyntaxKind.LessThanExpression;
			case SyntaxKind.LessThanExpression:
				return SyntaxKind.GreaterThanOrEqualExpression;
			case SyntaxKind.LessThanOrEqualExpression:
				return SyntaxKind.GreaterThanExpression;
			case SyntaxKind.LogicalOrExpression:
				return SyntaxKind.LogicalAndExpression;
			case SyntaxKind.LogicalAndExpression:
				return SyntaxKind.LogicalOrExpression;
			}
			throw new ArgumentOutOfRangeException ("op");
		}

		/// <summary>
		/// Returns true, if the specified operator is a relational operator
		/// </summary>
		public static bool IsRelationalOperator (SyntaxKind op)
		{
			switch (op) {
			case SyntaxKind.EqualsExpression:
			case SyntaxKind.NotEqualsExpression:
			case SyntaxKind.GreaterThanExpression:
			case SyntaxKind.GreaterThanOrEqualExpression:
			case SyntaxKind.LessThanExpression:
			case SyntaxKind.LessThanOrEqualExpression:
			case SyntaxKind.LogicalOrExpression:
			case SyntaxKind.LogicalAndExpression:
				return true;
			}
			return false;
		}

		/// <summary>
		/// Get negation of the condition operator
		/// </summary>
		/// <returns>
		/// negation of the specified condition operator, or BinaryOperatorType.Any if it's not a condition operator
		/// </returns>
		public static SyntaxKind NegateConditionOperator (SyntaxKind op)
		{
			switch (op) {
			case SyntaxKind.LogicalOrExpression:
				return SyntaxKind.LogicalAndExpression;
			case SyntaxKind.LogicalAndExpression:
				return SyntaxKind.LogicalOrExpression;
			}
			throw new ArgumentOutOfRangeException ("op");
		}

		public static bool AreConditionsEqual (ExpressionSyntax cond1, ExpressionSyntax cond2)
		{
			if (cond1 == null || cond2 == null)
				return false;
			return cond1.SkipParens ().IsEquivalentTo (cond2.SkipParens (), true);
		}
	}
}