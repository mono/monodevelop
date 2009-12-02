// NRefactoryEvaluator.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using DC = Mono.Debugging.Client;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;
using System.Reflection;
using Mono.Debugging.Client;

namespace Mono.Debugging.Evaluation
{
	public class NRefactoryEvaluator: ExpressionEvaluator
	{
		Dictionary<string,ValueReference> userVariables = new Dictionary<string, ValueReference> ();
		
		public override ValueReference Evaluate (EvaluationContext ctx, string exp, object expectedType)
		{
			if (exp.StartsWith ("?"))
				exp = exp.Substring (1).Trim ();
			if (exp.StartsWith ("var ")) {
				exp = exp.Substring (4).Trim (' ','\t');
				string var = null;
				for (int n=0; n<exp.Length; n++) {
					if (!char.IsLetterOrDigit (exp[n]) && exp[n] != '_') {
						var = exp.Substring (0, n);
						if (!exp.Substring (n).Trim (' ','\t').StartsWith ("="))
							var = null;
						break;
					}
					if (n == exp.Length - 1) {
						var = exp;
						exp = null;
						break;
					}
				}
				if (!string.IsNullOrEmpty (var))
					userVariables [var] = new UserVariableReference (ctx, var);
				if (exp == null)
					return null;
			}
			StringReader codeStream = new StringReader (exp);
			IParser parser = ParserFactory.CreateParser (SupportedLanguage.CSharp, codeStream);
			Expression expObj = parser.ParseExpression ();
			if (expObj == null)
				throw new EvaluatorException ("Could not parse expression '" + exp + "'");
			EvaluatorVisitor ev = new EvaluatorVisitor (ctx, exp, expectedType, userVariables);
			return (ValueReference) expObj.AcceptVisitor (ev, null);
		}
	}

	class EvaluatorVisitor: AbstractAstVisitor
	{
		EvaluationContext ctx;
		EvaluationOptions options;
		string name;
		object expectedType;
		Dictionary<string,ValueReference> userVariables;

		public EvaluatorVisitor (EvaluationContext ctx, string name, object expectedType, Dictionary<string,ValueReference> userVariables)
		{
			this.ctx = ctx;
			this.name = name;
			this.expectedType = expectedType;
			this.userVariables = userVariables;
			this.options = ctx.Options;
		}
		
		public override object VisitUnaryOperatorExpression (ICSharpCode.NRefactory.Ast.UnaryOperatorExpression unaryOperatorExpression, object data)
		{
			ValueReference vref = (ValueReference) unaryOperatorExpression.Expression.AcceptVisitor (this, null);
			object val = vref.ObjectValue;
			
			switch (unaryOperatorExpression.Op) {
				case UnaryOperatorType.BitNot: {
					long num = Convert.ToInt64 (val);
					num = ~num;
					val = Convert.ChangeType (num, val.GetType ());
					break;
				}
				case UnaryOperatorType.Minus: {
					long num = Convert.ToInt64 (val);
					num = -num;
					val = Convert.ChangeType (num, val.GetType ());
					break;
				}
				case UnaryOperatorType.Not:
					val = !(bool) val;
					break;
				case UnaryOperatorType.Plus:
					break;
				default:
					throw CreateNotSupportedError ();
			}
			
			return LiteralValueReference.CreateObjectLiteral (ctx, name, val);
		}
		
		public override object VisitTypeReference (ICSharpCode.NRefactory.Ast.TypeReference typeReference, object data)
		{
			object type = ctx.Adapter.GetType (ctx, typeReference.Type);
			if (type != null)
				return new TypeValueReference (ctx, type);
			else
				throw CreateParseError ("Unknown type: " + typeReference.Type);
		}
		
		public override object VisitTypeReferenceExpression (ICSharpCode.NRefactory.Ast.TypeReferenceExpression typeReferenceExpression, object data)
		{
			throw CreateNotSupportedError ();
		}

		public override object VisitTypeOfExpression (ICSharpCode.NRefactory.Ast.TypeOfExpression typeOfExpression, object data)
		{
			object type = ctx.Adapter.GetType (ctx, typeOfExpression.TypeReference.Type);
			object ob = ctx.Adapter.CreateTypeObject (ctx, type);
			if (ob != null)
				return LiteralValueReference.CreateTargetObjectLiteral (ctx, typeOfExpression.TypeReference.Type, ob);
			else
				throw CreateNotSupportedError ();
		}
		
		public override object VisitThisReferenceExpression (ICSharpCode.NRefactory.Ast.ThisReferenceExpression thisReferenceExpression, object data)
		{
			ValueReference val = ctx.Adapter.GetThisReference (ctx);
			if (val != null)
				return val;
			else
				throw CreateParseError ("'this' reference not available in the current evaluation context.");
		}
		
		public override object VisitPrimitiveExpression (ICSharpCode.NRefactory.Ast.PrimitiveExpression primitiveExpression, object data)
		{
			if (primitiveExpression.Value != null)
				return LiteralValueReference.CreateObjectLiteral (ctx, name, primitiveExpression.Value);
			else if (expectedType != null)
				return new NullValueReference (ctx, expectedType);
			else
				return new NullValueReference (ctx, ctx.Adapter.GetType (ctx, "System.Object"));
		}
		
		public override object VisitParenthesizedExpression (ICSharpCode.NRefactory.Ast.ParenthesizedExpression parenthesizedExpression, object data)
		{
			return parenthesizedExpression.Expression.AcceptVisitor (this, data);
		}
		
		public override object VisitObjectCreateExpression (ICSharpCode.NRefactory.Ast.ObjectCreateExpression objectCreateExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitInvocationExpression (ICSharpCode.NRefactory.Ast.InvocationExpression invocationExpression, object data)
		{
			if (!options.AllowMethodEvaluation)
				throw CreateNotSupportedError ();

			ValueReference target = null;
			string methodName;
			
			object[] argtypes = new object[invocationExpression.Arguments.Count];
			object[] args = new object[invocationExpression.Arguments.Count];
			for (int n=0; n<args.Length; n++) {
				Expression exp = invocationExpression.Arguments [n];
				ValueReference vref = (ValueReference) exp.AcceptVisitor (this, data);
				args [n] = vref.Value;
				argtypes [n] = ctx.Adapter.GetValueType (ctx, args [n]);
			}
			
			if (invocationExpression.TargetObject is MemberReferenceExpression) {
				MemberReferenceExpression field = (MemberReferenceExpression)invocationExpression.TargetObject;
				target = (ValueReference) field.TargetObject.AcceptVisitor (this, data);
				methodName = field.MemberName;
			} else if (invocationExpression.TargetObject is IdentifierExpression) {
				IdentifierExpression exp = (IdentifierExpression) invocationExpression.TargetObject;
				methodName = exp.Identifier;
				ValueReference vref = ctx.Adapter.GetThisReference (ctx);
				if (vref != null && ctx.Adapter.HasMethod (ctx, vref.Type, methodName, BindingFlags.Instance)) {
					// There is an instance method for 'this', although it may not have an exact signature match. Check it now.
					if (ctx.Adapter.HasMethod (ctx, vref.Type, methodName, argtypes, BindingFlags.Instance))
						target = vref;
					else {
						// There isn't an instance method with exact signature match.
						// If there isn't a static method, then use the instance method,
						// which will report the signature match error when invoked
						object etype = ctx.Adapter.GetEnclosingType (ctx);
						if (!ctx.Adapter.HasMethod (ctx, etype, methodName, argtypes, BindingFlags.Static))
							target = vref;
					}
				}
				else {
					if (ctx.Adapter.HasMethod (ctx, ctx.Adapter.GetEnclosingType (ctx), methodName, argtypes, BindingFlags.Instance))
						throw new EvaluatorException ("Can't invoke an instance method from a static method.");
					target = null;
				}
			}
			else
				throw CreateNotSupportedError ();

			object vtype = target != null ? target.Type : ctx.Adapter.GetEnclosingType (ctx);
			object vtarget = (target is TypeValueReference) || target == null ? null : target.Value;
			
			object res = ctx.Adapter.RuntimeInvoke (ctx, vtype, vtarget, methodName, argtypes, args);
			if (res != null)
				return LiteralValueReference.CreateTargetObjectLiteral (ctx, name, res);
			else
				return LiteralValueReference.CreateObjectLiteral (ctx, name, new LiteralExp ("No return value."));
		}
		
		public override object VisitInnerClassTypeReference (ICSharpCode.NRefactory.Ast.InnerClassTypeReference innerClassTypeReference, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitIndexerExpression (ICSharpCode.NRefactory.Ast.IndexerExpression indexerExpression, object data)
		{
			ValueReference val = (ValueReference) indexerExpression.TargetObject.AcceptVisitor (this, data);
			if (val is TypeValueReference)
				throw CreateNotSupportedError ();
			if (ctx.Adapter.IsArray (ctx, val.Value)) {
				int[] indexes = new int [indexerExpression.Indexes.Count];
				for (int n=0; n<indexes.Length; n++) {
					ValueReference vi = (ValueReference) indexerExpression.Indexes[n].AcceptVisitor (this, data);
					indexes [n] = (int) Convert.ChangeType (vi.ObjectValue, typeof(int));
				}
				return new ArrayValueReference (ctx, val.Value, indexes);
			}
			
			if (indexerExpression.Indexes.Count == 1) {
				ValueReference vi = (ValueReference) indexerExpression.Indexes[0].AcceptVisitor (this, data);
				vi = ctx.Adapter.GetIndexerReference (ctx, val.Value, vi.Value);
				if (vi != null)
					return vi;
			}
			
			throw CreateNotSupportedError ();
		}
		
		public override object VisitIdentifierExpression (ICSharpCode.NRefactory.Ast.IdentifierExpression identifierExpression, object data)
		{
			string name = identifierExpression.Identifier;
			
			// Look in user defined variables
			
			ValueReference userVar;
			if (userVariables.TryGetValue (name, out userVar))
				return userVar;
				
			// Look in variables

			ValueReference var = ctx.Adapter.GetLocalVariable (ctx, name);
			if (var != null)
				return var;

			// Look in parameters

			var = ctx.Adapter.GetParameter (ctx, name);
			if (var != null)
				return var;
			
			// Look in fields and properties

			ValueReference thisobj = ctx.Adapter.GetThisReference (ctx);
			object thistype = ctx.Adapter.GetEnclosingType (ctx);

			var = ctx.Adapter.GetMember (ctx, thistype, thisobj != null ? thisobj.Value : null, name);
			if (var != null)
				return var;

			// Look in types
			
			object vtype = ctx.Adapter.GetType (ctx, name);
			if (vtype != null)
				return new TypeValueReference (ctx, vtype);
			
			// Look in nested types
			
			vtype = ctx.Adapter.GetEnclosingType (ctx);
			if (vtype != null) {
				foreach (object ntype in ctx.Adapter.GetNestedTypes (ctx, vtype)) {
					if (TypeValueReference.GetTypeName (ctx.Adapter.GetTypeName (ctx, ntype)) == name)
						return new TypeValueReference (ctx, ntype);
				}
	
				string[] namespaces = ctx.Adapter.GetImportedNamespaces (ctx);
				if (namespaces.Length > 0) {
					// Look in namespaces
					foreach (string ns in namespaces) {
						vtype = ctx.Adapter.GetType (ctx, ns + "." + name);
						if (vtype != null)
							return new TypeValueReference (ctx, vtype);
					}
					foreach (string ns in namespaces) {
						if (ns == name || ns.StartsWith (name + "."))
							return new NamespaceValueReference (ctx, name);
					}
				}
			}
			throw CreateParseError ("Unknown identifier: {0}", name);
		}
		
		public override object VisitMemberReferenceExpression (MemberReferenceExpression memberReferenceExpression, object data)
		{
			ValueReference vref = (ValueReference) memberReferenceExpression.TargetObject.AcceptVisitor (this, data);
			ValueReference ch = vref.GetChild (memberReferenceExpression.MemberName);
			if (ch == null)
				throw CreateParseError ("Unknown member: {0}", memberReferenceExpression.MemberName);
			return ch;
		}
		
		public override object VisitConditionalExpression (ICSharpCode.NRefactory.Ast.ConditionalExpression conditionalExpression, object data)
		{
			ValueReference vc = (ValueReference) conditionalExpression.Condition.AcceptVisitor (this, data);
			bool cond = (bool) vc.ObjectValue;
			if (cond)
				return conditionalExpression.TrueExpression.AcceptVisitor (this, data);
			else
				return conditionalExpression.FalseExpression.AcceptVisitor (this, data);
		}
		
		public override object VisitClassReferenceExpression (ICSharpCode.NRefactory.Ast.ClassReferenceExpression classReferenceExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitCastExpression (ICSharpCode.NRefactory.Ast.CastExpression castExpression, object data)
		{
			ValueReference val = (ValueReference)castExpression.Expression.AcceptVisitor (this, data);
			TypeValueReference type = castExpression.CastTo.AcceptVisitor (this, data) as TypeValueReference;
			if (type == null)
				throw CreateParseError ("Invalid cast type.");
			object ob = ctx.Adapter.TryCast (ctx, val.Value, type.Type);
			if (ob == null) {
				if (castExpression.CastType == CastType.TryCast)
					return new NullValueReference (ctx, type.Type);
				else
					throw CreateParseError ("Invalid cast.");
			}
			return LiteralValueReference.CreateTargetObjectLiteral (ctx, name, ob);
		}
		
		public override object VisitBinaryOperatorExpression (ICSharpCode.NRefactory.Ast.BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			ValueReference left = (ValueReference) binaryOperatorExpression.Left.AcceptVisitor (this, data);
			return EvaluateBinaryOperatorExpression (left, binaryOperatorExpression.Right, binaryOperatorExpression.Op, data);
		}
		
		object EvaluateBinaryOperatorExpression (ValueReference left, ICSharpCode.NRefactory.Ast.Expression rightExp, BinaryOperatorType oper, object data)
		{
			// Shortcut ops
			
			switch (oper) {
				case BinaryOperatorType.LogicalAnd: {
					object val = left.ObjectValue;
					if (!(val is bool))
						throw CreateParseError ("Left operand of logical And must be a boolean");
					if (!(bool)val)
						return LiteralValueReference.CreateObjectLiteral (ctx, name, false);
					return rightExp.AcceptVisitor (this, data);
				}
				case BinaryOperatorType.LogicalOr: {
					object val = left.ObjectValue;
					if (!(val is bool))
						throw CreateParseError ("Left operand of logical Or must be a boolean");
					if ((bool)val)
						return LiteralValueReference.CreateObjectLiteral (ctx, name, true);
					return rightExp.AcceptVisitor (this, data);
				}
			}

			ValueReference right = (ValueReference) rightExp.AcceptVisitor (this, data);
			object val1 = left.ObjectValue;
			object val2 = right.ObjectValue;

			if (oper == BinaryOperatorType.Add || oper == BinaryOperatorType.Concat) {
				if (val1 is string || val2 is string) {
					if (!(val1 is string) && val1 != null)
						val1 = left.CallToString ();
					if (!(val2 is string) && val2 != null)
						val2 = right.CallToString ();
					return LiteralValueReference.CreateObjectLiteral (ctx, name, (string) val1 + (string) val2);
				}
			}
			
			if ((oper == BinaryOperatorType.ExclusiveOr) && (val1 is bool) && (val2 is bool))
				return LiteralValueReference.CreateObjectLiteral (ctx, name, (bool)val1 ^ (bool)val2);
			
			switch (oper) {
				case BinaryOperatorType.Equality:
					if (val1 == null || val2 == null)
						return LiteralValueReference.CreateObjectLiteral (ctx, name, val1 == val2);
					return LiteralValueReference.CreateObjectLiteral (ctx, name, val1.Equals (val2));
				case BinaryOperatorType.InEquality:
					if (val1 == null || val2 == null)
						return LiteralValueReference.CreateObjectLiteral (ctx, name, val1 != val2);
					return LiteralValueReference.CreateObjectLiteral (ctx, name, !val1.Equals (val2));
				case BinaryOperatorType.ReferenceEquality:
					return LiteralValueReference.CreateObjectLiteral (ctx, name, val1 == val2);
				case BinaryOperatorType.ReferenceInequality:
					return LiteralValueReference.CreateObjectLiteral (ctx, name, val1 != val2);
				case BinaryOperatorType.Concat:
					throw CreateParseError ("Invalid binary operator.");
			}
			
			if (val1 == null || val2 == null || (val1 is bool) || (val2 is bool))
				throw CreateParseError ("Invalid operands in binary operator");
			
			long v1, v2;
			
			try {
				v1 = Convert.ToInt64 (val1);
				v2 = Convert.ToInt64 (val2);
			} catch {
				throw CreateParseError ("Invalid operands in binary operator");
			}
			
			object res;
			
			switch (oper) {
				case BinaryOperatorType.Add: res = v1 + v2; break;
				case BinaryOperatorType.BitwiseAnd: res = v1 & v2; break;
				case BinaryOperatorType.BitwiseOr: res = v1 | v2; break;
				case BinaryOperatorType.ExclusiveOr: res = v1 ^ v2; break;
				case BinaryOperatorType.DivideInteger:
				case BinaryOperatorType.Divide: res = v1 / v2; break;
				case BinaryOperatorType.Modulus: res = v1 % v2; break;
				case BinaryOperatorType.Multiply: res = v1 * v2; break;
				case BinaryOperatorType.Power: res = v1 ^ v2; break;
				case BinaryOperatorType.ShiftLeft: res = v1 << (int)v2; break;
				case BinaryOperatorType.ShiftRight: res = v1 >> (int)v2; break;
				case BinaryOperatorType.Subtract: res = v1 - v2; break;
				case BinaryOperatorType.GreaterThan: res = v1 > v2; break;
				case BinaryOperatorType.GreaterThanOrEqual: res = v1 >= v2; break;
				case BinaryOperatorType.LessThan: res = v1 < v2; break;
				case BinaryOperatorType.LessThanOrEqual: res = v1 <= v2; break;
				default: throw CreateParseError ("Invalid binary operator.");
			}
			
			if (!(res is bool))
				res = (long) Convert.ChangeType (res, GetCommonType (v1, v2));
			return LiteralValueReference.CreateObjectLiteral (ctx, name, res);
		}
		
		Type GetCommonType (object v1, object v2)
		{
			int s1 = Marshal.SizeOf (v1);
			if (IsUnsigned (s1))
				s1 += 8;
			int s2 = Marshal.SizeOf (v2);
			if (IsUnsigned (s2))
				s2 += 8;
			if (s1 > s2)
				return v1.GetType ();
			else
				return v2.GetType ();
		}
		
		bool IsUnsigned (object v)
		{
			return (v is byte) || (v is ushort) || (v is uint) || (v is ulong);
		}
		
		public override object VisitBaseReferenceExpression (ICSharpCode.NRefactory.Ast.BaseReferenceExpression baseReferenceExpression, object data)
		{
			ValueReference thisobj = ctx.Adapter.GetThisReference (ctx);
			if (thisobj != null) {
				object baseob = ctx.Adapter.GetBaseValue (ctx, thisobj.Value);
				if (baseob == null)
					throw CreateParseError ("'base' reference not available.");
				return LiteralValueReference.CreateTargetObjectLiteral (ctx, name, baseob);
			}
			else
				throw CreateParseError ("'base' reference not available in static methods.");
		}
		
		public override object VisitAssignmentExpression (ICSharpCode.NRefactory.Ast.AssignmentExpression assignmentExpression, object data)
		{
			if (!options.AllowMethodEvaluation)
				throw CreateNotSupportedError ();
			
			ValueReference left = (ValueReference) assignmentExpression.Left.AcceptVisitor (this, data);
			
			if (assignmentExpression.Op == AssignmentOperatorType.Assign) {
				ValueReference right = (ValueReference) assignmentExpression.Right.AcceptVisitor (this, data);
				left.Value = right.Value;
			} else {
				BinaryOperatorType bop = BinaryOperatorType.None;
				switch (assignmentExpression.Op) {
					case AssignmentOperatorType.Add: bop = BinaryOperatorType.Add; break;
					case AssignmentOperatorType.BitwiseAnd: bop = BinaryOperatorType.BitwiseAnd; break;
					case AssignmentOperatorType.BitwiseOr: bop = BinaryOperatorType.BitwiseOr; break;
					case AssignmentOperatorType.ConcatString: bop = BinaryOperatorType.Concat; break;
					case AssignmentOperatorType.Divide: bop = BinaryOperatorType.Divide; break;
					case AssignmentOperatorType.DivideInteger: bop = BinaryOperatorType.DivideInteger; break;
					case AssignmentOperatorType.ExclusiveOr: bop = BinaryOperatorType.ExclusiveOr; break;
					case AssignmentOperatorType.Modulus: bop = BinaryOperatorType.Modulus; break;
					case AssignmentOperatorType.Multiply: bop = BinaryOperatorType.Multiply; break;
					case AssignmentOperatorType.Power: bop = BinaryOperatorType.Power; break;
					case AssignmentOperatorType.ShiftLeft: bop = BinaryOperatorType.ShiftLeft; break;
					case AssignmentOperatorType.ShiftRight: bop = BinaryOperatorType.ShiftRight; break;
					case AssignmentOperatorType.Subtract: bop = BinaryOperatorType.Subtract; break;
				}
				ValueReference val = (ValueReference) EvaluateBinaryOperatorExpression (left, assignmentExpression.Right, bop, data);
				left.Value = val.Value;
			}
			return left;
		}
		
		public override object VisitArrayCreateExpression (ICSharpCode.NRefactory.Ast.ArrayCreateExpression arrayCreateExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		#region Unsupported expressions
		
		public override object VisitPointerReferenceExpression (ICSharpCode.NRefactory.Ast.PointerReferenceExpression pointerReferenceExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitSizeOfExpression (ICSharpCode.NRefactory.Ast.SizeOfExpression sizeOfExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitTypeOfIsExpression (ICSharpCode.NRefactory.Ast.TypeOfIsExpression typeOfIsExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitYieldStatement (ICSharpCode.NRefactory.Ast.YieldStatement yieldStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitWithStatement (ICSharpCode.NRefactory.Ast.WithStatement withStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitVariableDeclaration (ICSharpCode.NRefactory.Ast.VariableDeclaration variableDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitUsing (ICSharpCode.NRefactory.Ast.Using @using, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitUsingStatement (ICSharpCode.NRefactory.Ast.UsingStatement usingStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitUsingDeclaration (ICSharpCode.NRefactory.Ast.UsingDeclaration usingDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitUnsafeStatement (ICSharpCode.NRefactory.Ast.UnsafeStatement unsafeStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitUncheckedStatement (ICSharpCode.NRefactory.Ast.UncheckedStatement uncheckedStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitTypeDeclaration (ICSharpCode.NRefactory.Ast.TypeDeclaration typeDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitTryCatchStatement (ICSharpCode.NRefactory.Ast.TryCatchStatement tryCatchStatement, object data)
		{
			throw CreateNotSupportedError ();
		}

		public override object VisitThrowStatement (ICSharpCode.NRefactory.Ast.ThrowStatement throwStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitTemplateDefinition (ICSharpCode.NRefactory.Ast.TemplateDefinition templateDefinition, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitSwitchStatement (ICSharpCode.NRefactory.Ast.SwitchStatement switchStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitSwitchSection (ICSharpCode.NRefactory.Ast.SwitchSection switchSection, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitStopStatement (ICSharpCode.NRefactory.Ast.StopStatement stopStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitStackAllocExpression (ICSharpCode.NRefactory.Ast.StackAllocExpression stackAllocExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitReturnStatement (ICSharpCode.NRefactory.Ast.ReturnStatement returnStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitResumeStatement (ICSharpCode.NRefactory.Ast.ResumeStatement resumeStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitRemoveHandlerStatement (ICSharpCode.NRefactory.Ast.RemoveHandlerStatement removeHandlerStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitReDimStatement (ICSharpCode.NRefactory.Ast.ReDimStatement reDimStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitRaiseEventStatement (ICSharpCode.NRefactory.Ast.RaiseEventStatement raiseEventStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitPropertySetRegion (ICSharpCode.NRefactory.Ast.PropertySetRegion propertySetRegion, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitPropertyGetRegion (ICSharpCode.NRefactory.Ast.PropertyGetRegion propertyGetRegion, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitPropertyDeclaration (ICSharpCode.NRefactory.Ast.PropertyDeclaration propertyDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitParameterDeclarationExpression (ICSharpCode.NRefactory.Ast.ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitOptionDeclaration (ICSharpCode.NRefactory.Ast.OptionDeclaration optionDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitOperatorDeclaration (ICSharpCode.NRefactory.Ast.OperatorDeclaration operatorDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitOnErrorStatement (ICSharpCode.NRefactory.Ast.OnErrorStatement onErrorStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitNamespaceDeclaration (ICSharpCode.NRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitNamedArgumentExpression (ICSharpCode.NRefactory.Ast.NamedArgumentExpression namedArgumentExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitMethodDeclaration (ICSharpCode.NRefactory.Ast.MethodDeclaration methodDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitLockStatement (ICSharpCode.NRefactory.Ast.LockStatement lockStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitLocalVariableDeclaration (ICSharpCode.NRefactory.Ast.LocalVariableDeclaration localVariableDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitLabelStatement (ICSharpCode.NRefactory.Ast.LabelStatement labelStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitInterfaceImplementation (ICSharpCode.NRefactory.Ast.InterfaceImplementation interfaceImplementation, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitIndexerDeclaration (ICSharpCode.NRefactory.Ast.IndexerDeclaration indexerDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitIfElseStatement (ICSharpCode.NRefactory.Ast.IfElseStatement ifElseStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitGotoStatement (ICSharpCode.NRefactory.Ast.GotoStatement gotoStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitGotoCaseStatement (ICSharpCode.NRefactory.Ast.GotoCaseStatement gotoCaseStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitForStatement (ICSharpCode.NRefactory.Ast.ForStatement forStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitForNextStatement (ICSharpCode.NRefactory.Ast.ForNextStatement forNextStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitForeachStatement (ICSharpCode.NRefactory.Ast.ForeachStatement foreachStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitFixedStatement (ICSharpCode.NRefactory.Ast.FixedStatement fixedStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitFieldDeclaration (ICSharpCode.NRefactory.Ast.FieldDeclaration fieldDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitExpressionStatement (ICSharpCode.NRefactory.Ast.ExpressionStatement expressionStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitExitStatement (ICSharpCode.NRefactory.Ast.ExitStatement exitStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitEventRemoveRegion (ICSharpCode.NRefactory.Ast.EventRemoveRegion eventRemoveRegion, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitEventRaiseRegion (ICSharpCode.NRefactory.Ast.EventRaiseRegion eventRaiseRegion, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitEventDeclaration (ICSharpCode.NRefactory.Ast.EventDeclaration eventDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitEventAddRegion (ICSharpCode.NRefactory.Ast.EventAddRegion eventAddRegion, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitErrorStatement (ICSharpCode.NRefactory.Ast.ErrorStatement errorStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitEraseStatement (ICSharpCode.NRefactory.Ast.EraseStatement eraseStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitEndStatement (ICSharpCode.NRefactory.Ast.EndStatement endStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitEmptyStatement (ICSharpCode.NRefactory.Ast.EmptyStatement emptyStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitElseIfSection (ICSharpCode.NRefactory.Ast.ElseIfSection elseIfSection, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitDoLoopStatement (ICSharpCode.NRefactory.Ast.DoLoopStatement doLoopStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitDirectionExpression (ICSharpCode.NRefactory.Ast.DirectionExpression directionExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitDestructorDeclaration (ICSharpCode.NRefactory.Ast.DestructorDeclaration destructorDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitDelegateDeclaration (ICSharpCode.NRefactory.Ast.DelegateDeclaration delegateDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitDefaultValueExpression (ICSharpCode.NRefactory.Ast.DefaultValueExpression defaultValueExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitDeclareDeclaration (ICSharpCode.NRefactory.Ast.DeclareDeclaration declareDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitContinueStatement (ICSharpCode.NRefactory.Ast.ContinueStatement continueStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitConstructorInitializer (ICSharpCode.NRefactory.Ast.ConstructorInitializer constructorInitializer, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitConstructorDeclaration (ICSharpCode.NRefactory.Ast.ConstructorDeclaration constructorDeclaration, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitCompilationUnit (ICSharpCode.NRefactory.Ast.CompilationUnit compilationUnit, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitCheckedStatement (ICSharpCode.NRefactory.Ast.CheckedStatement checkedStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitCatchClause (ICSharpCode.NRefactory.Ast.CatchClause catchClause, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitCaseLabel (ICSharpCode.NRefactory.Ast.CaseLabel caseLabel, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitBreakStatement (ICSharpCode.NRefactory.Ast.BreakStatement breakStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitAttributeSection (ICSharpCode.NRefactory.Ast.AttributeSection attributeSection, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitAttribute (ICSharpCode.NRefactory.Ast.Attribute attribute, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitAnonymousMethodExpression (ICSharpCode.NRefactory.Ast.AnonymousMethodExpression anonymousMethodExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitAddressOfExpression (ICSharpCode.NRefactory.Ast.AddressOfExpression addressOfExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitAddHandlerStatement (ICSharpCode.NRefactory.Ast.AddHandlerStatement addHandlerStatement, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		#endregion
		
		Exception CreateParseError (string message, params object[] args)
		{
			return new EvaluatorException (message, args);
		}
		
		Exception CreateNotSupportedError ()
		{
			return new NotSupportedExpressionException ();
		}
	}
}
