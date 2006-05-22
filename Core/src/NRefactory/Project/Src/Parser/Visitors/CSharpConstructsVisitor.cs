// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald" email="daniel@danielgrunwald.de"/>
//     <version>$Revision: 992 $</version>
// </file>

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using ICSharpCode.NRefactory.Parser.VB;
using ICSharpCode.NRefactory.Parser.AST;

namespace ICSharpCode.NRefactory.Parser
{
	/// <summary>
	/// Converts special C# constructs to use more general AST classes.
	/// </summary>
	public class CSharpConstructsVisitor : AbstractAstTransformer
	{
		// The following conversions are implemented:
		//   a == null -> a Is Nothing
		//   a != null -> a Is Not Nothing
		//   i++ / ++i as statement: convert to i += 1
		//   i-- / --i as statement: convert to i -= 1
		//   ForStatement -> ForNextStatement when for-loop is simple
		
		// The following conversions should be implemented in the future:
		//   if (Event != null) Event(this, bla); -> RaiseEvent Event(this, bla)
		
		public override object Visit(BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			if (binaryOperatorExpression.Op == BinaryOperatorType.Equality || binaryOperatorExpression.Op == BinaryOperatorType.InEquality) {
				if (IsNullLiteralExpression(binaryOperatorExpression.Left)) {
					Expression tmp = binaryOperatorExpression.Left;
					binaryOperatorExpression.Left = binaryOperatorExpression.Right;
					binaryOperatorExpression.Right = tmp;
				}
				if (IsNullLiteralExpression(binaryOperatorExpression.Right)) {
					if (binaryOperatorExpression.Op == BinaryOperatorType.Equality) {
						binaryOperatorExpression.Op = BinaryOperatorType.ReferenceEquality;
					} else {
						binaryOperatorExpression.Op = BinaryOperatorType.ReferenceInequality;
					}
				}
			}
			return base.Visit(binaryOperatorExpression, data);
		}
		
		bool IsNullLiteralExpression(Expression expr)
		{
			PrimitiveExpression pe = expr as PrimitiveExpression;
			if (pe == null) return false;
			return pe.Value == null;
		}
		
		
		public override object Visit(StatementExpression statementExpression, object data)
		{
			UnaryOperatorExpression uoe = statementExpression.Expression as UnaryOperatorExpression;
			if (uoe != null) {
				switch (uoe.Op) {
					case UnaryOperatorType.Increment:
					case UnaryOperatorType.PostIncrement:
						statementExpression.Expression = new AssignmentExpression(uoe.Expression, AssignmentOperatorType.Add, new PrimitiveExpression(1, "1"));
						break;
					case UnaryOperatorType.Decrement:
					case UnaryOperatorType.PostDecrement:
						statementExpression.Expression = new AssignmentExpression(uoe.Expression, AssignmentOperatorType.Subtract, new PrimitiveExpression(1, "1"));
						break;
				}
			}
			return base.Visit(statementExpression, data);
		}
		
		
		public override object Visit(IfElseStatement ifStatement, object data)
		{
			base.Visit(ifStatement, data);
			BinaryOperatorExpression boe = ifStatement.Condition as BinaryOperatorExpression;
			if (ifStatement.ElseIfSections.Count == 0
			    && ifStatement.FalseStatement.Count == 0
			    && ifStatement.TrueStatement.Count == 1
			    && boe != null
			    && boe.Op == BinaryOperatorType.ReferenceInequality
			    && (IsNullLiteralExpression(boe.Left) || IsNullLiteralExpression(boe.Right))
			   )
			{
				IdentifierExpression ident = boe.Left as IdentifierExpression;
				if (ident == null)
					ident = boe.Right as IdentifierExpression;
				StatementExpression se = ifStatement.TrueStatement[0] as StatementExpression;
				if (se == null) {
					BlockStatement block = ifStatement.TrueStatement[0] as BlockStatement;
					if (block != null && block.Children.Count == 1) {
						se = block.Children[0] as StatementExpression;
					}
				}
				if (ident != null && se != null) {
					InvocationExpression ie = se.Expression as InvocationExpression;
					if (ie != null &&
					    ie.TargetObject is IdentifierExpression &&
					    (ie.TargetObject as IdentifierExpression).Identifier == ident.Identifier)
					{
						ReplaceCurrentNode(new RaiseEventStatement(ident.Identifier, ie.Arguments));
					}
				}
			}
			return null;
		}
		
		public override object Visit(ForStatement forStatement, object data)
		{
			base.Visit(forStatement, data);
			ConvertForStatement(forStatement);
			return null;
		}
		
		void ConvertForStatement(ForStatement forStatement)
		{
			//   ForStatement -> ForNextStatement when for-loop is simple
			
			// only the following forms of the for-statement are allowed:
			// for (TypeReference name = start; name < oneAfterEnd; name += step)
			// for (name = start; name < oneAfterEnd; name += step)
			// for (TypeReference name = start; name <= end; name += step)
			// for (name = start; name <= end; name += step)
			// for (TypeReference name = start; name > oneAfterEnd; name -= step)
			// for (name = start; name > oneAfterEnd; name -= step)
			// for (TypeReference name = start; name >= end; name -= step)
			// for (name = start; name >= end; name -= step)
			
			// check if the form is valid and collect TypeReference, name, start, end and step
			if (forStatement.Initializers.Count != 1)
				return;
			if (forStatement.Iterator.Count != 1)
				return;
			StatementExpression statement = forStatement.Iterator[0] as StatementExpression;
			if (statement == null)
				return;
			AssignmentExpression iterator = statement.Expression as AssignmentExpression;
			if (iterator == null || (iterator.Op != AssignmentOperatorType.Add && iterator.Op != AssignmentOperatorType.Subtract))
				return;
			IdentifierExpression iteratorIdentifier = iterator.Left as IdentifierExpression;
			if (iteratorIdentifier == null)
				return;
			PrimitiveExpression stepExpression = iterator.Right as PrimitiveExpression;
			if (stepExpression == null || !(stepExpression.Value is int))
				return;
			int step = (int)stepExpression.Value;
			if (iterator.Op == AssignmentOperatorType.Subtract)
				step = -step;
			
			BinaryOperatorExpression condition = forStatement.Condition as BinaryOperatorExpression;
			if (condition == null || !(condition.Left is IdentifierExpression))
				return;
			if ((condition.Left as IdentifierExpression).Identifier != iteratorIdentifier.Identifier)
				return;
			Expression end;
			if (iterator.Op == AssignmentOperatorType.Subtract) {
				if (condition.Op == BinaryOperatorType.GreaterThanOrEqual) {
					end = condition.Right;
				} else if (condition.Op == BinaryOperatorType.GreaterThan) {
					end = Expression.AddInteger(condition.Right, 1);
				} else {
					return;
				}
			} else {
				if (condition.Op == BinaryOperatorType.LessThanOrEqual) {
					end = condition.Right;
				} else if (condition.Op == BinaryOperatorType.LessThan) {
					end = Expression.AddInteger(condition.Right, -1);
				} else {
					return;
				}
			}
			
			Expression start;
			TypeReference typeReference = null;
			LocalVariableDeclaration varDecl = forStatement.Initializers[0] as LocalVariableDeclaration;
			if (varDecl != null) {
				if (varDecl.Variables.Count != 1
				    || varDecl.Variables[0].Name != iteratorIdentifier.Identifier
				    || varDecl.Variables[0].Initializer == null)
					return;
				typeReference = varDecl.GetTypeForVariable(0);
				start = varDecl.Variables[0].Initializer;
			} else {
				statement = forStatement.Initializers[0] as StatementExpression;
				if (statement == null)
					return;
				AssignmentExpression assign = statement.Expression as AssignmentExpression;
				if (assign == null || assign.Op != AssignmentOperatorType.Assign)
					return;
				if (!(assign.Left is IdentifierExpression))
					return;
				if ((assign.Left as IdentifierExpression).Identifier != iteratorIdentifier.Identifier)
					return;
				start = assign.Right;
			}
			
			ReplaceCurrentNode(new ForNextStatement(typeReference, iteratorIdentifier.Identifier,
			                                        start, end,
			                                        (step == 1) ? null : new PrimitiveExpression(step, step.ToString(System.Globalization.NumberFormatInfo.InvariantInfo)),
			                                        forStatement.EmbeddedStatement, null));
		}
	}
}
