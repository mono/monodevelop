#region license
//
//	(C) 2005 - 2007 db4objects Inc. http://www.db4o.com
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

using Mono.Cecil;
using Mono.Cecil.Cil;

using Cecil.Decompiler.Ast;
using Cecil.Decompiler.Cil;

namespace Cecil.Decompiler {

	class ExpressionDecompiler : BaseInstructionVisitor {

		MethodDefinition method;
		AnnotationStore annotations;

		Stack<Expression> expression_stack = new Stack<Expression> ();
		HashSet<VariableReference> registers = new HashSet<VariableReference> ();

		PropertyDefinition array_length;

		public int Count {
			get { return expression_stack.Count; }
		}

		public ExpressionDecompiler (MethodDefinition method, AnnotationStore annotations)
		{
			this.method = method;
			this.annotations = annotations;
			this.array_length = new PropertyDefinition (
				"Length",
				new TypeReference ("Int32", "System", null, true),
				PropertyAttributes.SpecialName | PropertyAttributes.RTSpecialName);
		}

		public IEnumerable<VariableReference> GetRegisters ()
		{
			return registers;
		}

		public override void OnNop (Instruction instruction)
		{
		}

		public override void OnRet (Instruction instruction)
		{
		}

		public override void OnBr (Instruction instruction)
		{
		}

		public override void OnLeave (Instruction instruction)
		{
		}

		public override void OnEndfinally (Instruction instruction)
		{
		}

		public override void OnEndfilter (Instruction instruction)
		{
		}

		public override void OnStloc (Instruction instruction)
		{
			if (IsSkipped (instruction))
				return;

			PushVariableAssignement ((VariableReference) instruction.Operand);
		}

		public override void OnStloc_0 (Instruction instruction)
		{
			if (IsSkipped (instruction))
				return;

			PushVariableAssignement (0);
		}

		public override void OnStloc_1 (Instruction instruction)
		{
			if (IsSkipped (instruction))
				return;

			PushVariableAssignement (1);
		}

		public override void OnStloc_2 (Instruction instruction)
		{
			if (IsSkipped (instruction))
				return;

			PushVariableAssignement (2);
		}

		public override void OnStloc_3 (Instruction instruction)
		{
			if (IsSkipped (instruction))
				return;

			PushVariableAssignement (3);
		}

		bool IsSkipped (Instruction instruction)
		{
			return annotations.IsAnnotated (instruction, Annotation.Skip);
		}

		public override void OnStarg (Instruction instruction)
		{
			PushArgumentReference ((ParameterReference) instruction.Operand);
			PushAssignment (Pop (), Pop ());
		}

		void PushVariableAssignement (VariableReference variable)
		{
			PushVariableAssignement (variable.Index);
		}

		void PushVariableAssignement (int index)
		{
			PushVariableReference (index);
			PushAssignment (Pop (), Pop ());
		}

		public override void OnStsfld (Instruction instruction)
		{
			var field = (FieldReference) instruction.Operand;
			PushAssignment (new FieldReferenceExpression (null, field), Pop());
		}

		public override void OnStfld (Instruction instruction)
		{
			var field = (FieldReference) instruction.Operand;
			var expression = Pop ();
			var target = Pop ();
			PushAssignment (new FieldReferenceExpression (target, field), expression);
		}

		void PushAssignment (Expression left, Expression right)
		{
			Push (new AssignExpression (left, right));
		}

		public override void OnCallvirt (Instruction instruction)
		{
			OnCall (instruction);
		}

		public override void OnCastclass (Instruction instruction)
		{
			PushCastExpression ((TypeReference) instruction.Operand);
		}

		public override void OnIsinst (Instruction instruction)
		{
			Push (
				new SafeCastExpression (
					Pop (),
					(TypeReference) instruction.Operand));
		}

		public override void OnCall (Instruction instruction)
		{
			var method = (MethodReference) instruction.Operand;

			var arguments = PopRange (method.Parameters.Count);
			var target = method.HasThis ? Pop () : null;

			var invocation = new MethodInvocationExpression (
					new MethodReferenceExpression (target, method));

			AddRange (invocation.Arguments, arguments);

			Push (invocation);
		}

		public override void OnNewobj (Instruction instruction)
		{
			var constructor = (MethodReference) instruction.Operand;

			var arguments = PopRange (constructor.Parameters.Count);

			var @new = new ObjectCreationExpression (constructor, null, null);

			AddRange (@new.Arguments, arguments);

			Push (@new);
		}

		public override void OnInitobj (Instruction instruction)
		{
			var address = (AddressOfExpression) Pop ();

			var type = (TypeReference) instruction.Operand;

			var @new = new ObjectCreationExpression (null, type, null);

			Push (new AssignExpression (address.Expression, @new));
		}

		static void AddRange<T> (IList<T> list, IEnumerable<T> range)
		{
			foreach (var item in range)
				list.Add (item);
		}

		public override void OnDup (Instruction instruction)
		{
			// var expression = Pop ();
			// Push (expression);
			// Push (expression);
		}

		public override void OnPop (Instruction instruction)
		{
		}

		public override void OnThrow (Instruction instruction)
		{
		}

		public override void OnRethrow (Instruction instruction)
		{
		}

		public override void OnAdd (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.Add);
		}

		public override void OnSub (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.Subtract);
		}

		public override void OnMul (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.Multiply);
		}

		public override void OnDiv (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.Divide);
		}

		public void PushBinaryExpression (BinaryOperator op)
		{
			var right = Pop ();
			var left = Pop ();
			Push (new BinaryExpression (op, left, right));
		}

		public override void OnLdstr (Instruction instruction)
		{
			PushLiteral (instruction.Operand);
		}

		public override void OnLdc_R4 (Instruction instruction)
		{
			PushLiteral (Convert.ToSingle (instruction.Operand));
		}

		public override void OnLdc_R8 (Instruction instruction)
		{
			PushLiteral (Convert.ToDouble (instruction.Operand));
		}

		public override void OnLdc_I8 (Instruction instruction)
		{
			PushLiteral (Convert.ToInt64 (instruction.Operand));
		}

		public override void OnLdc_I4 (Instruction instruction)
		{
			PushLiteral (Convert.ToInt32 (instruction.Operand));
		}

		public override void OnLdc_I4_M1 (Instruction instruction)
		{
			PushLiteral (-1);
		}

		public override void OnLdc_I4_0 (Instruction instruction)
		{
			PushLiteral (0);
		}

		public override void OnLdc_I4_1 (Instruction instruction)
		{
			PushLiteral (1);
		}

		public override void OnLdc_I4_2 (Instruction instruction)
		{
			PushLiteral (2);
		}

		public override void OnLdc_I4_3 (Instruction instruction)
		{
			PushLiteral (3);
		}

		public override void OnLdc_I4_4 (Instruction instruction)
		{
			PushLiteral (4);
		}

		public override void OnLdc_I4_5 (Instruction instruction)
		{
			PushLiteral (5);
		}

		public override void OnLdc_I4_6 (Instruction instruction)
		{
			PushLiteral (6);
		}

		public override void OnLdc_I4_7 (Instruction instruction)
		{
			PushLiteral (7);
		}

		public override void OnLdc_I4_8 (Instruction instruction)
		{
			PushLiteral (8);
		}

		public override void OnLdloc_0 (Instruction instruction)
		{
			PushVariableReference (instruction, 0);
		}

		public override void OnLdloc_1 (Instruction instruction)
		{
			PushVariableReference (instruction, 1);
		}

		public override void OnLdloc_2 (Instruction instruction)
		{
			PushVariableReference (instruction, 2);
		}

		public override void OnLdloc_3 (Instruction instruction)
		{
			PushVariableReference (instruction, 3);
		}

		public override void OnLdloc (Instruction instruction)
		{
			PushVariableReference (instruction, (VariableReference) instruction.Operand);
		}

		public override void OnLdloca (Instruction instruction)
		{
			if (!ProcessRegister (instruction, (VariableReference) instruction.Operand))
				PushVariableReference ((VariableReference) instruction.Operand);

			PushAddressOf ();
		}

		void PushVariableReference (Instruction instruction, int index)
		{
			if (ProcessRegister (instruction, method.Body.Variables [index]))
				return;

			PushVariableReference (index);
		}

		void PushVariableReference (Instruction instruction, VariableReference variable)
		{
			if (ProcessRegister (instruction, variable))
				return;

			PushVariableReference (variable);
		}

		bool ProcessRegister (Instruction instruction, VariableReference variable)
		{
			if (!annotations.IsAnnotated (instruction, Annotation.Register))
				return false;

			registers.Add (variable);
			return true;
		}

		void PushVariableReference (int index)
		{
			PushVariableReference (method.Body.Variables [index]);
		}

		void PushVariableReference (VariableReference variable)
		{
			Push (new VariableReferenceExpression (variable));
		}

		void PushAddressOf ()
		{
			Push (new AddressOfExpression (Pop ()));
		}

		public override void OnNewarr (Instruction instruction)
		{
			var creation = new ArrayCreationExpression (
				(TypeReference) instruction.Operand,
				new BlockExpression ());

			creation.Dimensions.Add (Pop ());

			Push (creation);
		}

        public override void OnLdlen (Instruction instruction)
        {
			Push (new PropertyReferenceExpression (Pop (), array_length));
        }

		public override void OnLdelem_Any (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_I (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_I1 (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_I2 (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_I4 (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_I8 (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_R4 (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_R8 (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_Ref (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_U1 (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_U2 (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnLdelem_U4 (Instruction instruction)
		{
			PushArrayIndexer ();
		}

		public override void OnStelem_Any (Instruction instruction)
		{
			PushArrayStore ();
		}

		public override void OnStelem_I (Instruction instruction)
		{
			PushArrayStore ();
		}

		public override void OnStelem_I1 (Instruction instruction)
		{
			PushArrayStore ();
		}

		public override void OnStelem_Ref (Instruction instruction)
		{
			PushArrayStore ();
		}

		void PushArrayStore ()
		{
			var assign = new AssignExpression ();
			assign.Expression = Pop ();

			PushArrayIndexer ();

			assign.Target = Pop ();

			Push (assign);
		}

		void PushArrayIndexer ()
		{
			var indexer = new ArrayIndexerExpression ();
			indexer.Indices.Add (Pop ());
			indexer.Target = Pop ();

			Push (indexer);
		}

		public override void OnBox (Instruction instruction)
		{
		}

		public override void OnUnbox (Instruction instruction)
		{
			PushCastExpression ((TypeReference) instruction.Operand);
		}

		TypeReference Import (Type type)
		{
			return method.DeclaringType.Module.Import (type);
		}

		void PushCastExpression (TypeReference target_type)
		{
			Push (new CastExpression (Pop (), target_type));
		}

		void PushCastExpression (Type type)
		{
			PushCastExpression (Import (type));
		}

		public override void OnConv_I (Instruction instruction)
		{
			PushCastExpression (typeof (IntPtr));
		}

		public override void OnConv_I1 (Instruction instruction)
		{
			PushCastExpression (typeof (sbyte));
		}

		public override void OnConv_I2 (Instruction instruction)
		{
			PushCastExpression (typeof (short));
		}

		public override void OnConv_I4 (Instruction instruction)
		{
			PushCastExpression (typeof (int));
		}

		public override void OnConv_I8 (Instruction instruction)
		{
			PushCastExpression (typeof (long));
		}

		public override void OnConv_U (Instruction instruction)
		{
			PushCastExpression (typeof (UIntPtr));
		}

		public override void OnConv_U1 (Instruction instruction)
		{
			PushCastExpression (typeof (byte));
		}

		public override void OnConv_U2 (Instruction instruction)
		{
			PushCastExpression (typeof (ushort));
		}

		public override void OnConv_U4 (Instruction instruction)
		{
			PushCastExpression (typeof (uint));
		}

		public override void OnConv_U8 (Instruction instruction)
		{
			PushCastExpression (typeof (ulong));
		}

		public override void OnConv_R_Un (Instruction instruction)
		{
			PushCastExpression (typeof (float));
		}

		public override void OnConv_R4 (Instruction instruction)
		{
			PushCastExpression (typeof (float));
		}

		public override void OnConv_R8 (Instruction instruction)
		{
			PushCastExpression (typeof (double));
		}

		public override void OnCeq (Instruction instruction)
		{
			// XXX: ceq might be used for reference equality as well

			var right = Pop ();
			var left = Pop ();

			// simplify common expression patterns
			// ((x < y) == 0) => x >= y
			// ((x > y) == 0) => x <= y
			// ((x == y) == 0) => x != y
			// (BooleanMethod(x) == 0) => !BooleanMethod(x)
			if (IsBooleanExpression (left) && IsFalse (right)) {
				Negate (left);
			} else {
				Push (new BinaryExpression (BinaryOperator.ValueEquality, left, right));
			}
		}

		static bool IsFalse (Expression expression)
		{
			var literal = expression as LiteralExpression;
			if (literal == null)
				return false;

			return 0.Equals (literal.Value);
		}

		bool IsBooleanExpression (Expression expression)
		{
			switch (expression.CodeNodeType) {
			case CodeNodeType.BinaryExpression:
				return IsComparisonOperator (((BinaryExpression) expression).Operator);
			case CodeNodeType.MethodInvocationExpression:
				var reference = ((MethodInvocationExpression) expression).Method as MethodReferenceExpression;
				if (reference != null)
					return reference.Method.ReturnType.ReturnType.FullName == Constants.Boolean;

				break;
			}
			return false;
		}

		static bool IsComparisonOperator (BinaryOperator op)
		{
			switch (op) {
			case BinaryOperator.GreaterThan:
			case BinaryOperator.LessThan:
			case BinaryOperator.GreaterThanOrEqual:
			case BinaryOperator.LessThanOrEqual:
			case BinaryOperator.ValueEquality:
			case BinaryOperator.ValueInequality:
				return true;
			}
			return false;
		}

		public override void OnClt (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.LessThan);
		}

		public override void OnClt_Un (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.LessThan);
		}

		public override void OnCgt (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.GreaterThan);
		}

		public override void OnCgt_Un (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.GreaterThan);
		}

		public override void OnBeq (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.ValueEquality);
		}

		public override void OnBne_Un (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.ValueInequality);
		}

		public override void OnBle (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.LessThanOrEqual);
		}

		public override void OnBle_Un (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.LessThanOrEqual);
		}

		public override void OnBgt (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.GreaterThan);
		}

		public override void OnBgt_Un (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.GreaterThan);
		}

		public override void OnBge (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.GreaterThanOrEqual);
		}

		public override void OnBge_Un (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.GreaterThanOrEqual);
		}

		public override void OnBlt (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.LessThan);
		}

		public override void OnBlt_Un (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.LessThan);
		}

		public override void OnShr (Instruction instruction)
		{
 			PushBinaryExpression (BinaryOperator.RightShift);
		}

		public override void OnShr_Un (Instruction instruction)
		{
 			PushBinaryExpression (BinaryOperator.RightShift);
		}

		public override void OnShl (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.LeftShift);
		}

		public override void OnOr (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.BitwiseOr);
		}

		public override void OnAnd (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.BitwiseAnd);
		}

		public override void OnXor (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.BitwiseXor);
		}

		public override void OnRem (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.Modulo);
		}

		public override void OnRem_Un (Instruction instruction)
		{
			PushBinaryExpression (BinaryOperator.Modulo);
		}

		public override void OnNot (Instruction instruction)
		{
			PushUnaryExpression (UnaryOperator.BitwiseNot, Pop ());
		}

		public override void OnBrtrue (Instruction instruction)
		{
		}

		public override void OnBrfalse (Instruction instruction)
		{
			Negate ();
		}

		public override void OnSwitch (Instruction instruction)
		{
		}

		void PushArgumentReference (ParameterReference parameter)
		{
			Push (new ArgumentReferenceExpression (parameter));
		}

		public override void OnLdfld (Instruction instruction)
		{
			PushFieldReference (instruction, Pop ());
		}

		public override void OnLdsfld (Instruction instruction)
		{
			PushFieldReference (instruction);
		}

		void PushFieldReference (Instruction instruction)
		{
			PushFieldReference (instruction, null);
		}

		void PushFieldReference (Instruction instruction, Expression target)
		{
			Push (new FieldReferenceExpression (target, (FieldReference) instruction.Operand));
		}

		public override void OnLdflda (Instruction instruction)
		{
			PushFieldReference (instruction, Pop ());
			PushAddressOf ();
		}

		public override void OnLdsflda (Instruction instruction)
		{
			PushFieldReference (instruction);
			PushAddressOf ();
		}

		public override void OnLdnull (Instruction instruction)
		{
			PushLiteral (null);
		}

		public override void OnLdarg_0 (Instruction instruction)
		{
			PushArgumentReference (0);
		}

		public override void OnLdarg_1 (Instruction instruction)
		{
			PushArgumentReference (1);
		}

		public override void OnLdarg_2 (Instruction instruction)
		{
			PushArgumentReference (2);
		}

		public override void OnLdarg_3 (Instruction instruction)
		{
			PushArgumentReference (3);
		}

		public override void OnLdarg (Instruction instruction)
		{
			PushArgumentReference (((ParameterDefinition) instruction.Operand).Sequence);
		}

		public override void OnLdarga (Instruction instruction)
		{
			PushArgumentReference (((ParameterDefinition) instruction.Operand).Sequence);
			PushAddressOf ();
		}

		public override void OnLdtoken (Instruction instruction)
		{
			var type = instruction.Operand as TypeReference;
			if (type != null) {
				Push (new TypeReferenceExpression (type));
				return;
			}

			var method = instruction.Operand as MethodReference;
			if (method != null) {
				Push (new MethodReferenceExpression (null, method));
				return;
			}

			var field = instruction.Operand as FieldReference;
			if (field != null) {
				Push (new FieldReferenceExpression (null, field));
				return;
			}

			throw new NotSupportedException ();
		}

		public void Negate ()
		{
			Negate (Pop ());
		}

		public void Negate (Expression expression)
		{
			switch (expression.CodeNodeType) {
			case CodeNodeType.BinaryExpression:
				var binary = (BinaryExpression) expression;
				BinaryOperator op;
				if (TryGetInverseOperator (binary.Operator, out op)) {
					binary.Operator = op;
					Push (binary);
				} else {
					switch (binary.Operator) {
					case BinaryOperator.LogicalAnd:
						Negate (binary.Left);
						Negate (binary.Right);
						PushBinaryExpression (BinaryOperator.LogicalOr);
						break;
					case BinaryOperator.LogicalOr:
						Negate (binary.Left);
						Negate (binary.Right);
						PushBinaryExpression (BinaryOperator.LogicalAnd);
						break;
					default:
						PushNotExpression (expression);
						break;
					}
				}
				break;
			case CodeNodeType.UnaryExpression:
				var unary = (UnaryExpression) expression;
				switch (unary.Operator) {
				case UnaryOperator.LogicalNot:
					Push (unary.Operand);
					break;
				default:
					throw new ArgumentException ("expression");
				}
				break;
			case CodeNodeType.ConditionExpression:
				var condition = (ConditionExpression) expression;
				Negate (condition.Condition);
				condition.Condition = Pop ();
				Negate (condition.Then);
				condition.Then = Pop ();
				Negate (condition.Else);
				condition.Else = Pop ();

				var @else = condition.Then;
				condition.Then = condition.Else;
				condition.Else = @else;

				Push (condition);
				break;
			default:
				PushNotExpression (expression);
				break;
			}
		}

		void PushNotExpression (Expression expression)
		{
			PushUnaryExpression (UnaryOperator.LogicalNot, expression);
		}

		void PushUnaryExpression (UnaryOperator op, Expression expression)
		{
			Push (new UnaryExpression (op, expression));
		}

		static bool TryGetInverseOperator (BinaryOperator op, out BinaryOperator inverse)
		{
			switch (op) {
			case BinaryOperator.ValueEquality:
				inverse = BinaryOperator.ValueInequality;
				break;
			case BinaryOperator.ValueInequality:
				inverse = BinaryOperator.ValueEquality;
				break;
			case BinaryOperator.LessThan:
				inverse = BinaryOperator.GreaterThanOrEqual;
				break;
			case BinaryOperator.LessThanOrEqual:
				inverse = BinaryOperator.GreaterThan;
				break;
			case BinaryOperator.GreaterThan:
				inverse = BinaryOperator.LessThanOrEqual;
				break;
			case BinaryOperator.GreaterThanOrEqual:
				inverse = BinaryOperator.LessThan;
				break;
			default:
				inverse = op;
				return false;
			}

			return true;
		}

		void PushArgumentReference (int index)
		{
			if (method.HasThis) {
				if (index == 0) {
					Push (new ThisReferenceExpression ());
					return;
				}
				index -= 1; // the Parameters collection dos not contain the implict this argument
			}
			Push (new ArgumentReferenceExpression (method.Parameters [index]));
		}

		void PushLiteral (object value)
		{
			Push (new LiteralExpression (value));
		}

		public void Push (Expression expression)
		{
			if (null == expression)
				throw new ArgumentNullException ("expression");

			expression_stack.Push (expression);
		}

		public Expression Pop ()
		{
			return expression_stack.Pop ();
		}

		ExpressionCollection PopRange (int count)
		{
			var range = new ExpressionCollection ();
			for (int i = 0; i < count; ++i)
				range.Insert (0, Pop ());

			return range;
		}
	}
}
