#if NET_2_0
using System;
using System.Text;
using System.Collections;
using System.Diagnostics;

using RefParser = ICSharpCode.SharpRefactory.Parser;
using AST = ICSharpCode.SharpRefactory.Parser.AST;

namespace MonoDevelop.Debugger
{
	public class DebuggerASTVisitor : RefParser.AbstractASTVisitor
	{
                public override object Visit (AST.PrimitiveExpression primitiveExpression, object data)  {
			object v = primitiveExpression.Value;
			Type t = v.GetType();

			if (t == typeof (bool))
				return new BoolExpression ((bool)v);
			else if (t == typeof (long))
				return new NumberExpression ((long)v);
			else if (t == typeof (int))
				return new NumberExpression ((int)v);
			else
				throw new EvaluationException (String.Format ("unhandled primitive expression: `{0}'", primitiveExpression.ToString()));
		}

                public override object Visit (AST.BinaryOperatorExpression binaryOperatorExpression, object data) {
			throw new EvaluationException ("AST.TypeReferenceExpression not yet implemented");
		}

                public override object Visit (AST.ParenthesizedExpression parenthesizedExpression, object data) {
			return parenthesizedExpression.Expression.AcceptVisitor (this, data);
		}

                public override object Visit (AST.FieldReferenceExpression fieldReferenceExpression, object data) {
			return new MemberAccessExpression ((Expression)fieldReferenceExpression.TargetObject.AcceptVisitor (this, data),
							   fieldReferenceExpression.FieldName);
		}

                public override object Visit (AST.InvocationExpression invocationExpression, object data) {
			Expression[] arg_expr = new Expression[invocationExpression.Parameters.Count];
			int i = 0;
			foreach (AST.Expression pexpr in invocationExpression.Parameters)
				arg_expr[i++] = (Expression)pexpr.AcceptVisitor (this, data);

			return new InvocationExpression ((Expression)invocationExpression.TargetObject.AcceptVisitor (this, data),
							 arg_expr);
		}

                public override object Visit (AST.IdentifierExpression identifierExpression, object data) {
			return new SimpleNameExpression (identifierExpression.Identifier);
		}

                public override object Visit (AST.TypeReferenceExpression typeReferenceExpression, object data) {
			throw new EvaluationException ("AST.TypeReferenceExpression not yet implemented");
		}

                public override object Visit (AST.UnaryOperatorExpression unaryOperatorExpression, object data) {
			throw new EvaluationException ("AST.UnaryOperatorExpression not yet implemented");
		}

                public override object Visit (AST.AssignmentExpression assignmentExpression, object data) {
			throw new EvaluationException ("AST.AssignmentExpression not yet implemented");
		}

                public override object Visit (AST.SizeOfExpression sizeOfExpression, object data) {
			throw new EvaluationException ("AST.SizeOfExpression not yet implemented");
		}

                public override object Visit (AST.TypeOfExpression typeOfExpression, object data) {
			throw new EvaluationException ("AST.TypeOfExpression not yet implemented");
		}

                public override object Visit (AST.CheckedExpression checkedExpression, object data) {
			throw new EvaluationException ("AST.CheckedExpression not yet implemented");
		}

                public override object Visit (AST.UncheckedExpression uncheckedExpression, object data) {
			throw new EvaluationException ("AST.UncheckedExpression not yet implemented");
		}

                public override object Visit (AST.PointerReferenceExpression pointerReferenceExpression, object data) {
			throw new EvaluationException ("AST.PointerReferenceExpression not yet implemented");
		}


                public override object Visit (AST.CastExpression castExpression, object data) {
			throw new EvaluationException ("AST.CastExpression not yet implemented");
		}

                public override object Visit (AST.StackAllocExpression stackAllocExpression, object data) {
			throw new EvaluationException ("AST.StackAllocExpression not yet implemented");
		}

                public override object Visit (AST.IndexerExpression indexerExpression, object data) {
			throw new EvaluationException ("AST.IndexerExpression not yet implemented");
		}

                public override object Visit (AST.ThisReferenceExpression thisReferenceExpression, object data) {
		  return new ThisExpression ();
		}

                public override object Visit (AST.BaseReferenceExpression baseReferenceExpression, object data) {
		  return new BaseExpression ();
		}

                public override object Visit (AST.ObjectCreateExpression objectCreateExpression, object data) {
			throw new EvaluationException ("AST.ObjectCreateExpression not yet implemented");
		}

                public override object Visit (AST.ArrayCreationParameter arrayCreationParameter, object data) {
			throw new EvaluationException ("AST.ArrayCreationParameter not yet implemented");
		}

		public override object Visit (AST.ArrayCreateExpression arrayCreateExpression, object data) {
			throw new EvaluationException ("AST.ArrayCreateExpression not yet implemented");
		}

                public override object Visit (AST.ArrayInitializerExpression arrayInitializerExpression, object data) {
			throw new EvaluationException ("AST.ArrayInitializerExpression not yet implemented");
		}

                public override object Visit (AST.DirectionExpression directionExpression, object data) {
			throw new EvaluationException ("AST.DirectionExpression not yet implemented");
		}

                public override object Visit (AST.ConditionalExpression conditionalExpression, object data) {
			Expression test_expr = (Expression)conditionalExpression.TestCondition.AcceptVisitor (this, data);
			Expression true_expr = (Expression)conditionalExpression.TrueExpression.AcceptVisitor (this, data);
			Expression false_expr = (Expression)conditionalExpression.FalseExpression.AcceptVisitor (this, data);
			return new ConditionalExpression (test_expr, true_expr, false_expr);
		}

	}
}
#endif
