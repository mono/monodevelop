#region license
//
//	(C) 2007 - 2008 Novell, Inc. http://www.novell.com
//	(C) 2007 - 2008 Jb Evain http://evain.net
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;

using Mono.Cecil;

using Cecil.Decompiler.Ast;

namespace Cecil.Decompiler.Languages {

	public class CSharpWriter : BaseLanguageWriter {

		bool inside_binary;

		public CSharpWriter (ILanguage language, IFormatter formatter)
			: base (language, formatter)
		{
		}

		public override void Write (MethodDefinition method)
		{
			WriteMethodVisibility (method);
			WriteSpace ();
            
			if (method.IsStatic) {
				WriteKeyword ("static");
				WriteSpace ();
			}

			WriteMethodReturnType (method);

		    Write (method.Name);

		    WriteToken ("(");

			WriteParameters (method);

			WriteToken (")");

			WriteLine ();

			Write (method.Body.Decompile (language));
		}

		void WriteMethodVisibility (MethodDefinition method)
		{
			if (method.IsPrivate)
				WriteKeyword ("private");
			else if (method.IsPublic)
				WriteKeyword ("public");
			else if (method.IsFamily)
				WriteKeyword ("protected");
			else if (method.IsAssembly)
				WriteKeyword ("internal");
			else if (method.IsFamilyOrAssembly) {
				WriteKeyword ("protected");
				WriteSpace ();
				WriteKeyword ("internal");
			} else
				throw new NotSupportedException ();
		}

		void WriteMethodReturnType (MethodDefinition method)
		{
			WriteReference (method.ReturnType.ReturnType);
			WriteSpace ();
		}

		void WriteParameters (MethodDefinition method)
		{
			for (int i = 0; i < method.Parameters.Count; i++) {
				var parameter = method.Parameters [i];

				if (i > 0) {
					WriteToken (",");
					WriteSpace ();
				}

				WriteReference (parameter.ParameterType);
				WriteSpace ();
				Write (parameter.Name);
			}
		}

		public override void Write (Statement statement)
		{
			Visit (statement);
			WriteLine ();
		}

		public override void Write (Expression expression)
		{
			Visit (expression);
		}

		public override void VisitBlockStatement (BlockStatement node)
		{
			WriteBlock (() => Visit (node.Statements));
		}

		void WriteBlock (Action action)
		{
			WriteToken ("{");
			WriteLine ();
			Indent ();

			action ();

			Outdent ();
			WriteToken ("}");
			WriteLine ();
		}

		public override void VisitExpressionStatement (ExpressionStatement node)
		{
			Visit (node.Expression);
			WriteToken (";");
			WriteLine ();
		}

		public override void VisitVariableDeclarationExpression (VariableDeclarationExpression node)
		{
			var variable = node.Variable;

			WriteReference (variable.VariableType);
			WriteSpace ();
			Write (variable.Name);
		}

		public override void VisitAssignExpression (AssignExpression node)
		{
			Write (node.Target);
			WriteTokenBetweenSpace ("=");
			Write (node.Expression);
		}

		public override void VisitArgumentReferenceExpression (ArgumentReferenceExpression node)
		{
			Write (node.Parameter.Name);
		}

		public override void VisitVariableReferenceExpression (VariableReferenceExpression node)
		{
			Write (node.Variable.Name);
		}

		public override void VisitLiteralExpression (LiteralExpression node)
		{
			var value = node.Value;
			if (value == null) {
				WriteKeyword ("null");
				return;
			}

			switch (Type.GetTypeCode (value.GetType ())) {
			case TypeCode.Boolean:
				WriteKeyword ((bool) value ? "true" : "false");
				return;
			case TypeCode.Char:
				WriteLiteral ("'");
				WriteLiteral (value.ToString ());
				WriteLiteral ("'");
				return;
			case TypeCode.String:
				WriteLiteral ("\"");
				WriteLiteral (value.ToString ());
				WriteLiteral ("\"");
				return;
			// complete
			default:
				WriteLiteral (value.ToString ());
				return;
			}
		}

		public override void VisitMethodInvocationExpression (MethodInvocationExpression node)
		{
			Visit (node.Method);
			WriteToken ("(");
			VisitList (node.Arguments);
			WriteToken (")");
		}

		public override void VisitBlockExpression (BlockExpression node)
		{
			VisitList (node.Expressions);
		}

		void VisitList (IList<Expression> list)
		{
			for (int i = 0; i < list.Count; i++) {
				if (i > 0) {
					WriteToken (",");
					WriteSpace ();
				}

				Visit (list [i]);
			}
		}

		public override void VisitMethodReferenceExpression (MethodReferenceExpression node)
		{
			if (node.Target != null) {
				Visit (node.Target);
				WriteToken (".");
			}

			if (!node.Method.HasThis) {
				WriteReference (node.Method.DeclaringType);
				WriteToken (".");
			}

			Write (node.Method.Name);
		}

		public override void VisitThisReferenceExpression (ThisReferenceExpression node)
		{
			WriteKeyword ("this");
		}

		public override void VisitBaseReferenceExpression (BaseReferenceExpression node)
		{
			WriteKeyword ("base");
		}

		public override void VisitBinaryExpression (BinaryExpression node)
		{
			var was_inside = inside_binary;
			inside_binary = true;

			if (was_inside)
				WriteToken ("(");
			Visit (node.Left);
			WriteSpace ();
			Write (ToString (node.Operator));
			WriteSpace ();
			Visit (node.Right);
			if (was_inside)
				WriteToken (")");

			inside_binary = was_inside;
		}

		public override void VisitFieldReferenceExpression (FieldReferenceExpression node)
		{
			if (node.Target != null)
				Visit (node.Target);
			else
				WriteReference (node.Field.DeclaringType);

			WriteToken (".");
			Write (node.Field.Name);
		}

		static string ToString (BinaryOperator op)
		{
			switch (op) {
			case BinaryOperator.Add: return "+";
			case BinaryOperator.BitwiseAnd: return "&";
			case BinaryOperator.BitwiseOr: return "|";
			case BinaryOperator.BitwiseXor: return "^";
			case BinaryOperator.Divide: return "/";
			case BinaryOperator.GreaterThan: return ">";
			case BinaryOperator.GreaterThanOrEqual: return ">=";
			case BinaryOperator.LeftShift: return "<<";
			case BinaryOperator.LessThan: return "<";
			case BinaryOperator.LessThanOrEqual: return "<=";
			case BinaryOperator.LogicalAnd: return "&&";
			case BinaryOperator.LogicalOr: return "||";
			case BinaryOperator.Modulo: return "%";
			case BinaryOperator.Multiply: return "*";
			case BinaryOperator.RightShift: return ">>";
			case BinaryOperator.Subtract: return "-";
			case BinaryOperator.ValueEquality: return "==";
			case BinaryOperator.ValueInequality: return "!=";
			default: throw new ArgumentException ();
			}
		}

		public override void VisitUnaryExpression (UnaryExpression node)
		{
			bool is_post_op = IsPostUnaryOperator (node.Operator);

			if (!is_post_op)
				Write (ToString (node.Operator));
			
			Visit (node.Operand);

			if (is_post_op)
				Write (ToString (node.Operator));
		}

		static bool IsPostUnaryOperator (UnaryOperator op)
		{
			switch (op) {
			case UnaryOperator.PostIncrement:
			case UnaryOperator.PostDecrement:
				return true;
			default:
				return false;
			}
		}

		static string ToString (UnaryOperator op)
		{
			switch (op) {
			case UnaryOperator.BitwiseNot:
				return "~";
			case UnaryOperator.LogicalNot:
				return "!";
			case UnaryOperator.Negate:
				return "-";
			case UnaryOperator.PostDecrement:
			case UnaryOperator.PreDecrement:
				return "--";
			case UnaryOperator.PostIncrement:
			case UnaryOperator.PreIncrement:
				return "++";
			default: throw new ArgumentException ();
			}
		}

		void WriteReference (TypeReference reference)
		{
			formatter.WriteReference (ToString (reference), reference);
		}

		static string ToString (TypeReference type)
		{
			var spec = type as TypeSpecification;
			if (spec != null)
				return ToString (spec);

			if (type.Namespace != "System")
				return type.Name;

			switch (type.Name) {
			case "Decimal": return "decimal";
			case "Single": return "float";
			case "Byte": return "byte";
			case "SByte": return "sbyte";
			case "Char": return "char";
			case "Double": return "double";
			case "Boolean": return "bool";
			case "Int16": return "short";
			case "Int32": return "int";
			case "Int64": return "long";
			case "UInt16": return "ushort";
			case "UInt32": return "uint";
			case "UInt64": return "ulong";
			case "String": return "string";
			case "Void": return "void";
			case "Object": return "object";
			default: return type.Name;
			}
		}

		static string ToString (TypeSpecification specification)
		{
			var pointer = specification as PointerType;
			if (pointer != null)
				return ToString (specification.ElementType) + "*";

			var reference = specification as ReferenceType;
			if (reference != null)
				return ToString (specification.ElementType) + "&";

			var array = specification as ArrayType;
			if (array != null)
				return ToString (specification.ElementType) + "[]";

			var generic = specification as GenericInstanceType;
			if (generic != null)
				return ToString (generic);

			throw new NotSupportedException ();
		}

		static string ToString (GenericInstanceType generic)
		{
			var name = ToString (generic.ElementType);

			var signature = new StringBuilder ();
			signature.Append (name.Substring (0, name.Length - 2));
			signature.Append ("<");

			for (int i = 0; i < generic.GenericArguments.Count; i++) {
				if (i > 0)
					signature.Append (", ");

				signature.Append (ToString (generic.GenericArguments [i]));
			}

			signature.Append (">");
			return signature.ToString ();
		}

		public override void VisitGotoStatement (GotoStatement node)
		{
			WriteKeyword ("goto");
			WriteSpace ();
			Write (node.Label);
			WriteToken (";");
			WriteLine ();
		}

		public override void VisitLabeledStatement (LabeledStatement node)
		{
			Outdent ();
			Write (node.Label);
			WriteToken (":");
			WriteLine ();
			Indent ();
		}

		public override void VisitIfStatement (IfStatement node)
		{
			WriteKeyword ("if");
			WriteSpace ();
			WriteBetweenParenthesis (node.Condition);
			WriteLine ();

			Visit (node.Then);

			if (node.Else == null)
				return;

			WriteKeyword ("else");
			WriteLine ();

			Visit (node.Else);
		}

		public override void VisitContinueStatement (ContinueStatement node)
		{
			WriteKeyword ("continue");
			WriteToken (";");
			WriteLine ();
		}

		public override void VisitBreakStatement (BreakStatement node)
		{
			WriteKeyword ("break");
			WriteToken (";");
			WriteLine ();
		}

		void WriteBetweenParenthesis (Expression expression)
		{
			WriteToken ("(");
			Visit (expression);
			WriteToken (")");
		}

		public override void VisitConditionExpression (ConditionExpression node)
		{
			WriteToken ("(");
			Visit (node.Condition);
			WriteTokenBetweenSpace ("?");
			Visit (node.Then);
			WriteTokenBetweenSpace (":");
			Visit (node.Else);
			WriteToken (")");
		}

		public override void VisitNullCoalesceExpression (NullCoalesceExpression node)
		{
			Visit (node.Condition);
			WriteTokenBetweenSpace ("??");
			Visit (node.Expression);
		}

		void WriteTokenBetweenSpace (string token)
		{
			WriteSpace ();
			WriteToken (token);
			WriteSpace ();
		}

		public override void VisitThrowStatement (ThrowStatement node)
		{
			WriteKeyword ("throw");
			if (node.Expression != null) {
				WriteSpace ();
				Visit (node.Expression);
			}
			WriteToken (";");
			WriteLine ();
		}

		public override void VisitReturnStatement (ReturnStatement node)
		{
			WriteKeyword ("return");

			if (node.Expression != null) {
				WriteSpace ();
				Visit (node.Expression);
			}

			WriteToken (";");
			WriteLine ();
		}

		public override void VisitSwitchStatement (SwitchStatement node)
		{
			WriteKeyword ("switch");

			WriteSpace ();

			WriteToken ("(");
			Visit (node.Expression);
			WriteToken (")");
			WriteLine ();

			WriteBlock (() => Visit (node.Cases));
		}

		public override void VisitConditionCase (ConditionCase node)
		{
			WriteKeyword ("case");
			WriteSpace ();
			Visit (node.Condition);
			WriteToken (":");
			WriteLine ();

			Visit (node.Body);
		}

		public override void VisitDefaultCase (DefaultCase node)
		{
			WriteKeyword ("default");
			WriteToken (":");
			WriteLine ();

			Visit (node.Body);
		}

		public override void VisitWhileStatement (WhileStatement node)
		{
			WriteKeyword ("while");
			WriteSpace ();
			WriteBetweenParenthesis (node.Condition);
			WriteLine ();
			Visit (node.Body);
		}

		public override void VisitDoWhileStatement (DoWhileStatement node)
		{
			WriteKeyword ("do");
			WriteLine ();
			Visit (node.Body);
			WriteKeyword ("while");
			WriteSpace ();
			WriteBetweenParenthesis (node.Condition);
			WriteToken (";");
			WriteLine ();
		}

		void VisitExpressionStatementExpression (Statement statement)
		{
			var expression_statement = statement as ExpressionStatement;
			if (expression_statement == null)
				throw new ArgumentException ();

			Visit (expression_statement.Expression);
		}

		public override void VisitForStatement (ForStatement node)
		{
			WriteKeyword ("for");
			WriteSpace ();
			WriteToken ("(");
			VisitExpressionStatementExpression (node.Initializer);
			WriteToken (";");
			WriteSpace ();
			Visit (node.Condition);
			WriteToken (";");
			WriteSpace ();
			VisitExpressionStatementExpression (node.Increment);
			WriteToken (")");
			WriteLine ();
			Visit (node.Body);
		}

		public override void VisitForEachStatement (ForEachStatement node)
		{
			WriteKeyword ("foreach");
			WriteSpace ();
			WriteToken ("(");
			Visit (node.Variable);
			WriteSpace ();
			WriteKeyword ("in");
			WriteSpace ();
			Visit (node.Expression);
			WriteToken (")");
			WriteLine ();
			Visit (node.Body);
		}

		public override void VisitCatchClause (CatchClause node)
		{
			WriteKeyword ("catch");

			if (node.Type.FullName != Constants.Object) {
				WriteSpace ();
				WriteToken ("(");
				if (node.Variable != null)
					Visit (node.Variable);
				else
					WriteReference (node.Type);
				WriteToken (")");
			}

			WriteLine ();
			Visit (node.Body);
		}

		public override void VisitTryStatement (TryStatement node)
		{
			WriteKeyword ("try");
			WriteLine ();
			Visit (node.Try);
			Visit (node.CatchClauses);

			if (node.Finally != null) {
				WriteKeyword ("finally");
				WriteLine ();
				Visit (node.Finally);
			}
		}

		public override void VisitArrayCreationExpression (ArrayCreationExpression node)
		{
			WriteKeyword ("new");
			WriteSpace ();
			WriteReference (node.Type);
			WriteToken ("[");
			Visit (node.Dimensions);
			WriteToken ("]");
		}

		public override void VisitArrayIndexerExpression (ArrayIndexerExpression node)
		{
			Visit (node.Target);
			WriteToken ("[");
			Visit (node.Indices);
			WriteToken ("]");
		}

		public override void VisitCastExpression (CastExpression node)
		{
			WriteToken ("(");
			WriteReference (node.TargetType);
			WriteToken (")");
			Visit (node.Expression);
		}

		public override void VisitSafeCastExpression (SafeCastExpression node)
		{
			Visit (node.Expression);
			WriteSpace ();
			WriteKeyword ("as");
			WriteSpace ();
			WriteReference (node.TargetType);
		}

		public override void VisitCanCastExpression (CanCastExpression node)
		{
			Visit (node.Expression);
			WriteSpace ();
			WriteKeyword ("is");
			WriteSpace ();
			WriteReference (node.TargetType);
		}

		public override void VisitAddressOfExpression (AddressOfExpression node)
		{
			WriteToken ("&");
			Visit (node.Expression);
		}

		public override void VisitObjectCreationExpression (ObjectCreationExpression node)
		{
			WriteKeyword ("new");
			WriteSpace ();
			WriteReference (node.Constructor != null ? node.Constructor.DeclaringType : node.Type);
			WriteToken ("(");
			Visit (node.Arguments);
			WriteToken (")");
		}

		public override void VisitPropertyReferenceExpression (PropertyReferenceExpression node)
		{
			if (node.Target != null)
				Visit (node.Target);
			else
				WriteReference (node.Property.DeclaringType);

			WriteToken (".");
			Write (node.Property.Name);
		}

		public override void VisitTypeReferenceExpression (TypeReferenceExpression node)
		{
			WriteReference (node.Type);
		}

		public override void VisitTypeOfExpression (TypeOfExpression node)
		{
			WriteKeyword ("typeof");
			WriteToken ("(");
			WriteReference (node.Type);
			WriteToken (")");
		}
	}
}
