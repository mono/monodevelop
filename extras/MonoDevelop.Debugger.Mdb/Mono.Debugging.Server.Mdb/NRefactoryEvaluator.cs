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
using Mono.Debugger;
using Mono.Debugger.Languages;
using DC = Mono.Debugging.Client;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Visitors;
using ICSharpCode.NRefactory.Ast;

namespace DebuggerServer
{
	public class NRefactoryEvaluator: ExpressionEvaluator
	{
		public override ValueReference Evaluate (StackFrame frame, string exp, EvaluationOptions options)
		{
			StringReader codeStream = new StringReader (exp);
			IParser parser = ParserFactory.CreateParser (SupportedLanguage.CSharp, codeStream);
			Expression expObj = parser.ParseExpression ();
			EvaluatorVisitor ev = new EvaluatorVisitor (frame, exp, options);
			return (ValueReference) expObj.AcceptVisitor (ev, null);
		}
	}
	
	class EvaluatorVisitor: AbstractAstVisitor
	{
		StackFrame frame;
		string name;
		EvaluationOptions options;
		
		public EvaluatorVisitor (StackFrame frame, string name, EvaluationOptions options)
		{
			this.frame = frame;
			this.name = name;
			this.options = options;
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
			
			return new LiteralValueReference (frame.Thread, name, val);
		}
		
		public override object VisitTypeReference (ICSharpCode.NRefactory.Ast.TypeReference typeReference, object data)
		{
			TargetType type = frame.Language.LookupType (typeReference.SystemType);
			if (type != null)
				return new TypeValueReference (frame.Thread, type);
			else
				throw CreateParseError ("Unknown type: " + typeReference.Type);
		}
		
		public override object VisitTypeReferenceExpression (ICSharpCode.NRefactory.Ast.TypeReferenceExpression typeReferenceExpression, object data)
		{
			throw CreateNotSupportedError ();
		}

		public override object VisitTypeOfExpression (ICSharpCode.NRefactory.Ast.TypeOfExpression typeOfExpression, object data)
		{
			TargetObject ob = ObjectUtil.GetTypeOf (frame, typeOfExpression.TypeReference.SystemType);
			if (ob != null)
				return new LiteralValueReference (frame.Thread, typeOfExpression.TypeReference.SystemType, ob);
			else
				throw CreateNotSupportedError ();
		}
		
		public override object VisitThisReferenceExpression (ICSharpCode.NRefactory.Ast.ThisReferenceExpression thisReferenceExpression, object data)
		{
			if (frame.Method != null && frame.Method.HasThis) {
				DC.ObjectValueFlags flags = DC.ObjectValueFlags.Field | DC.ObjectValueFlags.ReadOnly;
				TargetVariable var = frame.Method.GetThis (frame.Thread);
				return new VariableReference (frame, var, flags);
			}
			else
				throw CreateParseError ("'this' reference not available in static methods");
		}
		
		public override object VisitPrimitiveExpression (ICSharpCode.NRefactory.Ast.PrimitiveExpression primitiveExpression, object data)
		{
			if (primitiveExpression.Value != null)
				return new LiteralValueReference (frame.Thread, name, primitiveExpression.Value);
			else if (options.ExpectedType != null)
				return new NullValueReference (frame.Thread, options.ExpectedType);
			else
				return new NullValueReference (frame.Thread, frame.Language.ObjectType);
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
			if (!options.CanEvaluateMethods)
				throw CreateNotSupportedError ();
				
			ValueReference target = null;
			string methodName;
			
			if (invocationExpression.TargetObject is FieldReferenceExpression) {
				FieldReferenceExpression field = (FieldReferenceExpression)invocationExpression.TargetObject;
				target = (ValueReference) field.TargetObject.AcceptVisitor (this, data);
				methodName = field.FieldName;
			} else if (invocationExpression.TargetObject is IdentifierExpression) {
				IdentifierExpression exp = (IdentifierExpression) invocationExpression.TargetObject;
				methodName = exp.Identifier;
				target = null;
			}
			else
				throw CreateNotSupportedError ();
			
			TargetType[] argtypes = new TargetType [invocationExpression.Arguments.Count];
			TargetObject[] args = new TargetObject [invocationExpression.Arguments.Count];
			for (int n=0; n<args.Length; n++) {
				Expression exp = invocationExpression.Arguments [n];
				ValueReference vref = (ValueReference) exp.AcceptVisitor (this, data);
				args [n] = vref.Value;
				argtypes [n] = args [n].Type;
			}
			
			// Locate the method
			
			bool allowStatic = false, allowInstance = false;
			
			TargetStructType type = null;
			
			if (target == null) {
				if (frame.Method != null) {
					type = frame.Method.GetDeclaringType (frame.Thread);
					allowStatic = true;
					allowInstance = frame.Method.HasThis;
				}
			} else if (target is TypeValueReference) {
				TypeValueReference tv = (TypeValueReference) target;
				allowInstance = false;
				allowStatic = true;
				type = tv.Type as TargetStructType;
			}
			else {
				allowInstance = true;
				allowStatic = false;
				type = target.Value.Type as TargetStructType;
			}
			
			if (type == null)
				throw CreateParseError ("Unknown method: {0}", methodName);
			
			TargetFunctionType method = OverloadResolve (methodName, type, argtypes, allowInstance, allowStatic);

			TargetMethodSignature sig = method.GetSignature (frame.Thread);

			TargetObject[] objs = new TargetObject [args.Length];
			for (int i = 0; i < args.Length; i++) {
				objs [i] = TargetObjectConvert.ImplicitConversionRequired (
					frame.Thread, args [i], sig.ParameterTypes [i]);
			}

			TargetStructObject thisobj = null;
			
			if (!method.IsStatic) {
				if (target != null)
					thisobj = target.Value as TargetStructObject;
				else if (frame.Method.HasThis) {
					TargetVariable var = frame.Method.GetThis (frame.Thread);
					thisobj = (TargetStructObject) var.GetObject (frame);
				}
			}
			
			TargetObject obj = Server.Instance.RuntimeInvoke (frame.Thread, method, thisobj, objs);
			return new LiteralValueReference (frame.Thread, name, obj);
		}
		
		public TargetFunctionType OverloadResolve (string methodName, TargetStructType type, TargetType[] argtypes, bool allowInstance, bool allowStatic)
		{
			List<TargetFunctionType> candidates = new List<TargetFunctionType> ();

			foreach (MemberReference mem in ObjectUtil.GetTypeMembers (frame.Thread, type, false, false, false, true, ReqMemberAccess.All)) {
				TargetMethodInfo met = (TargetMethodInfo) mem.Member;
				if (met.Name == methodName && met.Type.ParameterTypes.Length == argtypes.Length && (met.IsStatic && allowStatic || !met.IsStatic && allowInstance))
					candidates.Add (met.Type);
			}
			
			TargetFunctionType candidate;
			if (candidates.Count == 1) {
				candidate = (TargetFunctionType) candidates [0];
				string error;
				if (IsApplicable (candidate, argtypes, out error))
					return candidate;

				throw CreateParseError ("The best overload of method `{0}' has some invalid arguments:\n{1}", methodName, error);
			}

			if (candidates.Count == 0)
				throw CreateParseError ("No overload of method `{0}' has {1} arguments.", methodName, argtypes.Length);

			candidate = OverloadResolve (argtypes, candidates);

			if (candidate == null)
				throw CreateParseError ("Ambiguous method `{0}'; need to use full name", methodName);

			return candidate;
		}

		public bool IsApplicable (TargetFunctionType method, TargetType[] types, out string error)
		{
			TargetMethodSignature sig = method.GetSignature (frame.Thread);

			for (int i = 0; i < types.Length; i++) {
				TargetType param_type = sig.ParameterTypes [i];

				if (param_type == types [i])
					continue;

				if (TargetObjectConvert.ImplicitConversionExists (frame.Thread, types [i], param_type))
					continue;

				error = String.Format (
					"Argument {0}: Cannot implicitly convert `{1}' to `{2}'",
					i, types [i].Name, param_type.Name);
				return false;
			}

			error = null;
			return true;
		}

		TargetFunctionType OverloadResolve (TargetType[] argtypes, List<TargetFunctionType> candidates)
		{
			// Ok, no we need to find an exact match.
			TargetFunctionType match = null;
			foreach (TargetFunctionType method in candidates) {
				string error;
				if (!IsApplicable (method, argtypes, out error))
					continue;

				// We need to find exactly one match
				if (match != null)
					return null;

				match = method;
			}

			return match;
		}
		
		public override object VisitInnerClassTypeReference (ICSharpCode.NRefactory.Ast.InnerClassTypeReference innerClassTypeReference, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitIndexerExpression (ICSharpCode.NRefactory.Ast.IndexerExpression indexerExpression, object data)
		{
			ValueReference val = (ValueReference) indexerExpression.TargetObject.AcceptVisitor (this, data);
			TargetObject ob = val.Value;
			TargetArrayObject arr = ob as TargetArrayObject;
			if (arr != null) {
				int[] indexes = new int [indexerExpression.Indexes.Count];
				for (int n=0; n<indexes.Length; n++) {
					ValueReference vi = (ValueReference) indexerExpression.Indexes [n].AcceptVisitor (this, data);
					indexes [n] = (int) Convert.ChangeType (vi.ObjectValue, typeof(int));
				}
				return new ArrayValueReference (frame.Thread, arr, indexes);
			}
			
			if (indexerExpression.Indexes.Count == 1) {
				ValueReference vi = (ValueReference) indexerExpression.Indexes [0].AcceptVisitor (this, data);
				IndexerValueReference idx = IndexerValueReference.CreateIndexerValueReference (frame.Thread, ob, vi.Value);
				if (idx != null)
					return idx;
			}
			
			throw CreateNotSupportedError ();
		}
		
		public override object VisitIdentifierExpression (ICSharpCode.NRefactory.Ast.IdentifierExpression identifierExpression, object data)
		{
			string name = identifierExpression.Identifier;
			
			// Look in variables
			
			foreach (TargetVariable var in frame.Method.GetLocalVariables (frame.Thread)) {
				if (var.Name == name)
					return new VariableReference (frame, var, DC.ObjectValueFlags.Variable);
			}
			
			// Look in parameters
			
			foreach (TargetVariable var in frame.Method.GetParameters (frame.Thread))
				if (var.Name == name)
					return new VariableReference (frame, var, DC.ObjectValueFlags.Parameter);
			
			// Look in fields and properties
			
			TargetStructObject thisobj = null;
			
			if (frame.Method.HasThis) {
				TargetObject ob = frame.Method.GetThis (frame.Thread).GetObject (frame);
				thisobj = ObjectUtil.GetRealObject (frame.Thread, ob) as TargetStructObject;
			}
			
			TargetStructType type = frame.Method.GetDeclaringType (frame.Thread);
			
			foreach (MemberReference mem in ObjectUtil.GetTypeMembers (frame.Thread, type, thisobj==null, true, true, false, ReqMemberAccess.All)) {
				if (mem.Member.Name != name)
					continue;
				if (mem.Member is TargetFieldInfo) {
					TargetFieldInfo field = (TargetFieldInfo) mem.Member;
					return new FieldReference (frame.Thread, thisobj, mem.DeclaringType, field);
				}
				if (mem.Member is TargetPropertyInfo) {
					TargetPropertyInfo prop = (TargetPropertyInfo) mem.Member;
					if (prop.CanRead)
						return new PropertyReference (frame.Thread, prop, thisobj);
				}
			}
			
			// Look in types
			
			TargetType vtype = frame.Language.LookupType (name);
			if (vtype != null)
				return new TypeValueReference (frame.Thread, vtype);
			
			if (frame.Method != null && frame.Method.HasLineNumbers) {
				string[] namespaces = frame.Method.GetNamespaces ();
				
				if (namespaces != null) {
					// Look in types from included namespaces
				
					foreach (string ns in namespaces) {
						vtype = frame.Language.LookupType (ns + "." + name);
						if (vtype != null)
							return new TypeValueReference (frame.Thread, vtype);
					}
				
					// Look in namespaces
				
					foreach (string ns in namespaces) {
						if (ns == name || ns.StartsWith (name + "."))
							return new NamespaceValueReference (frame, name);
					}
				}
			}

			throw CreateParseError ("Unknwon identifier: {0}", name);
		}
		
		public override object VisitFieldReferenceExpression (ICSharpCode.NRefactory.Ast.FieldReferenceExpression fieldReferenceExpression, object data)
		{
			ValueReference vref = (ValueReference) fieldReferenceExpression.TargetObject.AcceptVisitor (this, data);
			ValueReference ch = vref.GetChild (fieldReferenceExpression.FieldName);
			if (ch == null)
				throw CreateParseError ("Unknown member: {0}", fieldReferenceExpression.FieldName);
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
			ValueReference val = (ValueReference) castExpression.Expression.AcceptVisitor (this, data);
			TypeValueReference type = castExpression.CastTo.AcceptVisitor (this, data) as TypeValueReference;
			if (type == null)
				throw CreateParseError ("Invalid cast type.");
			TargetObject ob = TargetObjectConvert.Cast (frame, val.Value, type.Type);
			if (ob == null) {
				if (castExpression.CastType == CastType.TryCast)
					return new NullValueReference (frame.Thread, type.Type);
				else
					throw CreateParseError ("Invalid cast.");
			}
			return ob;
		}
		
		public override object VisitBinaryOperatorExpression (ICSharpCode.NRefactory.Ast.BinaryOperatorExpression binaryOperatorExpression, object data)
		{
			ValueReference left = (ValueReference) binaryOperatorExpression.Left.AcceptVisitor (this, data);
			
			// Shortcut ops
			
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.LogicalAnd: {
					if (!(bool)left.ObjectValue)
						return left;
					return binaryOperatorExpression.Right.AcceptVisitor (this, data);
				}
				case BinaryOperatorType.LogicalOr: {
					if ((bool)left.ObjectValue)
						return left;
					return binaryOperatorExpression.Right.AcceptVisitor (this, data);
				}
			}
			
			ValueReference right = (ValueReference) binaryOperatorExpression.Right.AcceptVisitor (this, data);

			if (binaryOperatorExpression.Op == BinaryOperatorType.Add || binaryOperatorExpression.Op == BinaryOperatorType.Concat) {
				if (left.Type == frame.Language.StringType || right.Type == frame.Language.StringType)
					return new LiteralValueReference (frame.Thread, name, left.CallToString () + right.CallToString ());
			}

			object val1 = left.ObjectValue;
			object val2 = right.ObjectValue;
			
			if ((binaryOperatorExpression.Op == BinaryOperatorType.ExclusiveOr) && (val1 is bool) && !(val2 is bool))
				return new LiteralValueReference (frame.Thread, name, (bool)val1 ^ (bool)val2);
			
			switch (binaryOperatorExpression.Op) {
				case BinaryOperatorType.Equality:
					return new LiteralValueReference (frame.Thread, name, val1.Equals (val2));
				case BinaryOperatorType.InEquality:
					return new LiteralValueReference (frame.Thread, name, !val1.Equals (val2));
				case BinaryOperatorType.ReferenceEquality:
					return new LiteralValueReference (frame.Thread, name, val1 == val2);
				case BinaryOperatorType.ReferenceInequality:
					return new LiteralValueReference (frame.Thread, name, val1 != val2);
				case BinaryOperatorType.Concat:
					throw CreateParseError ("Invalid binary operator.");
			}
			
			long v1 = Convert.ToInt64 (left.ObjectValue);
			long v2 = Convert.ToInt64 (right.ObjectValue);
			object res;
			
			switch (binaryOperatorExpression.Op) {
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
			return new LiteralValueReference (frame.Thread, name, res);
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
			if (frame.Method != null && frame.Method.HasThis) {
				TargetVariable var = frame.Method.GetThis (frame.Thread);
				TargetClassObject ob = (TargetClassObject) var.GetObject (frame);
				TargetObject baseob = ob.GetParentObject (frame.Thread);
				if (baseob == null)
					throw CreateParseError ("'base' reference not available.");
				return new LiteralValueReference (frame.Thread, name, baseob);
			}
			else
				throw CreateParseError ("'base' reference not available in static methods.");
		}
		
		public override object VisitAssignmentExpression (ICSharpCode.NRefactory.Ast.AssignmentExpression assignmentExpression, object data)
		{
			throw CreateNotSupportedError ();
		}
		
		public override object VisitArrayInitializerExpression (ICSharpCode.NRefactory.Ast.ArrayInitializerExpression arrayInitializerExpression, object data)
		{
			throw CreateNotSupportedError ();
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
			return new EvaluatorException ("Expression not supported.");
		}
		
		public string[] GetNamespaces ()
		{
			Method method = frame.Method;
			if ((method == null) || !method.HasLineNumbers)
				return null;

			return method.GetNamespaces ();
		}
		
        public static string MakeFqn (string nsn, string name)
        {
			if (nsn == "")
				return name;
			return String.Concat (nsn, ".", name);
        }
	}
}
