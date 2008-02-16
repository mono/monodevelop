//
// Decompiler.cs
//
// Author:
//   Andrea Paatz <andrea@icsharpcode.net>
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Text;
using System.Collections.Generic;

using Mono.Cecil;
using Mono.Cecil.Cil;

using MonoDevelop.AssemblyBrowser.Dom;

namespace MonoDevelop.AssemblyBrowser
{
	public class Decompiler
	{
		enum OperationType 
		{
			Primary        = 1,
			Unary          = 2,
			Multiplicative = 3,
			Additive       = 4,
			Shift          = 6,
			Relational     = 7,
			Equality       = 8,
			LogicalAnd     = 9,
			LogicalXor     = 10,
			LogicalOr      = 11,
			ConditionalAnd = 12,
			ConditionalOr  = 13,
			Conditional    = 14,
			Assignment     = 15
		}
		
		class Operand
		{
			string stringValue;
			OperationType operationType;
			
			public string StringValue {
				get { return stringValue; }
			}
			
			public OperationType OperationType {
				get { return operationType; }
			}
			
			public Operand (string stringValue) : this (stringValue, OperationType.Primary)
			{
			}
			
			public Operand (string stringValue, OperationType operationType) 
			{
				this.stringValue   = stringValue;
				this.operationType = operationType;
			}
		} // class Operand
		
		class Statement 
		{
			static int labelcount = 1;
			int line = -1;
			string s;
			string label = "";
			
			public int Line {
				get {
					return line;
				}
				set {
					if (line < 0)
						line = value;
				}
			}
			
			public bool HasLabel() 
			{
				return label != "";
			}
			
			public string Label {
				get {
					return label;
				}
				set {
					label = value;
				}
			}
			
			public string StringValue {
				get {
					return s;
				}
				set {
					s = value;
				}
			}
			
			public static string Nextlabel () 
			{
				return ("l" + labelcount++);
			}
			
			public Statement (int line, string text)
			{
				this.line = line;
				this.s = text;
			}
			
			public Statement ()
			{
			}
		} // Statement
		
		string GetOperatorChar (Code c)
		{
			switch (c) {
			case Code.Add:
				return "+";
			case Code.Mul:
				return "*";
			case Code.Sub:
				return "-";
			case Code.Div:
				return "/";
			case Code.Rem:
				return "%";
			case Code.And:
				return "&";
			case Code.Or:
				return "|";
			case Code.Xor:
				return "^";
			case Code.Shl:
				return "<<";
			case Code.Shr:
				return ">>";
			case Code.Beq:
			case Code.Ceq:
				return "==";
			case Code.Bgt:
			case Code.Cgt:
				return ">";
			case Code.Bge:
				return ">=";
			case Code.Blt:
			case Code.Clt:
				return "<";
			case Code.Ble:
				return "<=";
			default:
				return "error " + c;
			}
		}
		
		OperationType GetOperatorPrecedence (Code c)
		{
			switch (c) {
			case Code.Add:
			case Code.Sub:
				return OperationType.Additive;
			case Code.Mul:
			case Code.Div:
			case Code.Rem:
				return OperationType.Multiplicative;
			case Code.And:
				return OperationType.LogicalAnd;
			case Code.Or:
				return OperationType.LogicalOr;
			case Code.Xor:
				return OperationType.LogicalXor;
			case Code.Shl:
			case Code.Shr:
				return OperationType.Shift;
			case Code.Beq:
			case Code.Bgt:
			case Code.Blt:
			case Code.Bge:
			case Code.Ble:
			case Code.Ceq:
			case Code.Cgt:
			case Code.Clt:
				return OperationType.Relational;
			default:
				return OperationType.Assignment;
			}
		}
		
		int GetLoadArgumentNumber (Code c, bool isStatic)
		{
			int result = 0;
			switch (c) {
			case Code.Ldarg_0:
				result = 0;
				break;
			case Code.Ldarg_1:
				result = 1;
				break;
			case Code.Ldarg_2:
				result = 2;
				break;
			case Code.Ldarg_3:
				result = 3;
				break;
			}
			if (!isStatic) {
				result--;
			}
			return result;
		}
		
		string GetVariableName (Code c)
		{
			switch (c) {
			case Code.Ldloc_0:
			case Code.Stloc_0:
				return "loc0";
			case Code.Ldloc_1:
			case Code.Stloc_1:
				return "loc1";
			case Code.Ldloc_2:
			case Code.Stloc_2:
				return "loc2";
			case Code.Ldloc_3:
			case Code.Stloc_3:
				return "loc3";
			}
			return "error" + c;
		}
		
		string GetConst (Code c)
		{
			switch (c) {
			case Code.Ldc_I4_M1:
				return "-1";
			case Code.Ldc_I4_0:
				return "0";
			case Code.Ldc_I4_1:
				return "1";
			case Code.Ldc_I4_2:
				return "2";
			case Code.Ldc_I4_3:
				return "3";
			case Code.Ldc_I4_4:
				return "4";
			case Code.Ldc_I4_5:
				return "5";
			case Code.Ldc_I4_6:
				return "6";
			case Code.Ldc_I4_7:
				return "7";
			case Code.Ldc_I4_8:
				return "8";
			case Code.Ldnull:
				return "null";
			}
			return "error" + c;
		}
		
		Dictionary<int, Statement> statements = new Dictionary<int, Statement>();
		
		void SetStatement (Statement statement)
		{
			if (!statements.ContainsKey (statement.Line)) {
				statements.Add (statement.Line, statement);
			} else {
				statements[statement.Line].StringValue = statement.StringValue;
			}
		}
		
		public string Decompile(MonoDevelop.Ide.Gui.Pads.ITreeNavigator navigator)
		{
			Statement current = new Statement();
			StringBuilder result = new StringBuilder ();
			Stack<Operand> stack = new Stack<Operand>();
			DomCecilMethod method = navigator.DataItem as DomCecilMethod;
			foreach (Instruction instruction in method.MethodDefinition.Body.Instructions) {
				string arg1, arg2, name;
				bool fullname = false;
				int i;
				try {
					MethodReference calledMethod;
					switch (instruction.OpCode.Code) {
					case Code.Box:
						break;
					case Code.Add:
					case Code.Sub:
					case Code.Div:
					case Code.Mul:
					case Code.Rem:
					case Code.And:
					case Code.Or:
					case Code.Xor:
					case Code.Shl:
					case Code.Shr:
					case Code.Ceq:
					case Code.Cgt:
					case Code.Clt:
						OperationType precedence = GetOperatorPrecedence (instruction.OpCode.Code);
						if (stack.Peek().OperationType > precedence)
							arg2 = "(" + stack.Pop().StringValue + ")";
						else
							arg2 = stack.Pop().StringValue;
						if (stack.Peek().OperationType > precedence)
							arg1 = "(" + stack.Pop().StringValue + ")";
						else
							arg1 = stack.Pop().StringValue;
						stack.Push(new Operand(arg1 + ' ' + GetOperatorChar(instruction.OpCode.Code) + ' ' + arg2, precedence));
						break;
					case Code.Castclass:
						if (stack.Peek().OperationType > OperationType.Assignment)
							arg1 = "(" + stack.Pop().StringValue + ")";
						else
							arg1 = stack.Pop().StringValue;
						stack.Push(new Operand("(" +  instruction.Operand.ToString() + ")" + arg1, OperationType.Assignment));
						break;
					case Code.Ldarg_0:
					case Code.Ldarg_1:
					case Code.Ldarg_2:
					case Code.Ldarg_3:
						current.Line = instruction.Offset;
						i = GetLoadArgumentNumber(instruction.OpCode.Code, method.IsStatic);
						if (i >= 0)
							stack.Push(new Operand(method.MethodDefinition.Parameters[i].Name.ToString()));
						break;
					case Code.Ldarg_S:
						current.Line = instruction.Offset;
						name = instruction.Operand.ToString();
						stack.Push(new Operand(name));
						break;
					case Code.Starg_S:
						name = instruction.Operand.ToString();
						current.StringValue = (name + " = " + stack.Pop().StringValue + ";");
						SetStatement (current);
						current = new Statement();
						break;
					case Code.Stloc_0:
					case Code.Stloc_1:
					case Code.Stloc_2:
					case Code.Stloc_3:
						current.StringValue = (GetVariableName (instruction.OpCode.Code) + " = " + stack.Pop().StringValue + ";");
						SetStatement (current);
						current = new Statement();
						break;
					case Code.Stloc_S:
						name = instruction.Operand.ToString();
						current.StringValue = (name + " = " + stack.Pop().StringValue + ";");
						SetStatement (current);
						current = new Statement();
						break;
					case Code.Stfld:
					case Code.Stsfld:
						FieldReference field = (FieldReference)instruction.Operand;
						current.StringValue = field.Name + " = " + stack.Pop().StringValue + ";";
						SetStatement (current);
						current = new Statement();
						break;
					case Code.Stelem_I:
					case Code.Stelem_I1:
					case Code.Stelem_I2:
					case Code.Stelem_I4:
					case Code.Stelem_I8:
					case Code.Stelem_R4:
					case Code.Stelem_R8:
					case Code.Stelem_Ref:
						arg2 = stack.Pop().StringValue;
						arg1 = stack.Pop().StringValue;
						current.StringValue = stack.Pop ().StringValue + "[" + arg1 + "] = " + arg2;
						SetStatement (current);
						current = new Statement ();
						break;
					case Code.Ldelem_I: // hope the only difference between them is the Type of the Array Elements
					case Code.Ldelem_I1:
					case Code.Ldelem_I2:
					case Code.Ldelem_I4:
					case Code.Ldelem_I8:
					case Code.Ldelem_R4:
					case Code.Ldelem_R8:
					case Code.Ldelem_Ref:
						arg1 = stack.Pop ().StringValue;
						stack.Push (new Operand (stack.Pop ().StringValue + "[" + arg1 + "]"));
						break;
					case Code.Ldloc_0:
					case Code.Ldloc_1:
					case Code.Ldloc_2:
					case Code.Ldloc_3:
						current.Line = instruction.Offset;
						stack.Push (new Operand (GetVariableName (instruction.OpCode.Code)));
						break;
					case Code.Ldloc_S:
						current.Line = instruction.Offset;
						name = instruction.Operand.ToString ();
						stack.Push (new Operand (name));
						break;
					case Code.Ldstr:
						current.Line = instruction.Offset;
						name = instruction.Operand.ToString ();
						stack.Push (new Operand ('"' + name + '"'));
						break;
					case Code.Ldc_I4_M1:
					case Code.Ldc_I4_0:
					case Code.Ldc_I4_1:
					case Code.Ldc_I4_2:
					case Code.Ldc_I4_3:
					case Code.Ldc_I4_4:
					case Code.Ldc_I4_5:
					case Code.Ldc_I4_6:
					case Code.Ldc_I4_7:
					case Code.Ldc_I4_8:
					case Code.Ldnull:
						current.Line = instruction.Offset;
						stack.Push (new Operand (GetConst (instruction.OpCode.Code)));
						break;
					case Code.Ldc_I4_S:
						current.Line = instruction.Offset;
						name = instruction.Operand.ToString ();
						stack.Push (new Operand (name));
						break;
					case Code.Ldc_I4:
						current.Line = instruction.Offset;
						name = instruction.Operand.ToString ();
						stack.Push (new Operand (name));
						break;
					case Code.Ldftn:
						current.Line = instruction.Offset;
						calledMethod = (MethodReference)instruction.Operand;
						name = calledMethod.ToString ();
						stack.Push (new Operand (name));
						break;
					case Code.Ldfld:
					case Code.Ldsfld:
						field = (FieldReference)instruction.Operand;
						current.Line = instruction.Offset;
						stack.Push (new Operand (field.Name));
						break;
					case Code.Call:
						current.Line = instruction.Offset;
						calledMethod = (MethodReference)instruction.Operand;
						name = calledMethod.ToString ();
						fullname = (method.MethodDefinition.DeclaringType != calledMethod.DeclaringType);
						if (CallType (name) == "System.Void") {
							current.StringValue = MethodCall (stack, instruction, fullname) + ";";
							SetStatement (current);
							current = new Statement ();
						} else {
							current.Line = instruction.Offset;
							stack.Push (new Operand (MethodCall (stack, instruction, fullname)));
						}
						break;
					case Code.Callvirt:
						calledMethod = (MethodReference)instruction.Operand;
						name = calledMethod.ToString ();
						if (CallType (name) == "System.Void") {
							current.StringValue = MethodCall (stack, instruction, false) + ";";
							arg1 = stack.Pop ().StringValue; // TODO: check for precedences
							current.StringValue = arg1 + '.' + current.StringValue;
							SetStatement (current);
							current = new Statement ();
						} else {
							current.Line = instruction.Offset;
							string s = MethodCall (stack, instruction, fullname);
							arg1 = stack.Pop ().StringValue; // TODO: check for precedences
							stack.Push (new Operand (arg1 + '.' + s));
						}
						break;
					case Code.Pop:
						current.StringValue = stack.Pop ().StringValue + ";";
						SetStatement (current);
						current = new Statement ();
						break;
					case Code.Newarr:
						current.Line = instruction.Offset;
						name = instruction.Operand.ToString ();
						StringBuilder str = new StringBuilder ();
						str.Append ("new ");
						str.Append (name);
						str.Append ("[");
						str.Append (stack.Pop ().StringValue);
						str.Append ("]");
						stack.Push (new Operand (str.ToString()));
						break;
					case Code.Newobj:
						current.Line = instruction.Offset;
						calledMethod = (MethodReference)instruction.Operand;
						fullname = (method.MethodDefinition.DeclaringType != calledMethod.DeclaringType);
						str = new StringBuilder ();
						if (fullname) {
							str.Append (calledMethod.DeclaringType.FullName);
						} else {
							str.Append (calledMethod.DeclaringType.Name);
						}
						str.Append('(');
						if (calledMethod.Parameters.Count > 0) {
							Operand[] op = new Operand[calledMethod.Parameters.Count];
							for (i = calledMethod.Parameters.Count - 1; i >= 0; --i) {
								op [i] = stack.Pop ();
							}
							str.Append (op[0].StringValue);
							for (i = 1; i < calledMethod.Parameters.Count; ++i) {
								str.Append (", ");
								str.Append (op[i].StringValue);
							}
						}
						str.Append (")");
						stack.Push (new Operand ("new " + str));
						break;
					case Code.Throw:
						current.StringValue = "throw "+ stack.Pop ().StringValue + ";";
						SetStatement (current);
						current = new Statement ();
						break;
					case Code.Beq:
					case Code.Bgt:
					case Code.Bge:
					case Code.Blt:
					case Code.Ble:
						precedence = GetOperatorPrecedence (instruction.OpCode.Code);
						if (stack.Peek ().OperationType > precedence)
							arg2 = "(" + stack.Pop ().StringValue + ")";
						else
							arg2 = stack.Pop().StringValue;
						if (stack.Peek ().OperationType > precedence)
							arg1 = "(" + stack.Pop ().StringValue + ")";
						else
							arg1 = stack.Pop().StringValue;
						stack.Push (new Operand (arg1 + ' ' + GetOperatorChar (instruction.OpCode.Code) + ' ' + arg2, precedence));
						goto case Code.Br;
					case Code.Br:
					case Code.Brfalse:
					case Code.Brtrue:
						if (instruction.OpCode.Code == Code.Br)
							current.Line = instruction.Offset;
						Mono.Cecil.Cil.Instruction instr = (Mono.Cecil.Cil.Instruction)instruction.Operand;
						int target = instr.Offset;
						string label = Statement.Nextlabel ();
						if (statements.ContainsKey (target)) {
							if(statements[target].HasLabel ())
								label = statements[target].Label;
							else
								statements[target].Label = label;
						} else {
							Statement s = new Statement ();
							s.Label = label;
							statements.Add (target, s);
						}
						name  = "goto " + label + ";";
						if (instruction.OpCode.Code != Code.Br) {
							string not = (instruction.OpCode.Code == Code.Brfalse) ? "!" : "";
							name = "if (" + not + stack.Pop ().StringValue + ") " + name;
						}
						current.StringValue = name;
						SetStatement (current);
						current = new Statement ();
						break;
					case Code.Ret:
						Console.WriteLine (method.ReturnType.Name);
						if (method.ReturnType.Name != "Void") {
							current.StringValue = ("return " + stack.Pop ().StringValue + ";");
							SetStatement (current);
							current = new Statement ();
						} else {
							current.Line = instruction.Offset;
							current.StringValue = ("return;");
							SetStatement (current);
							current = new Statement ();
						}
						break;
					default:
						current.Line = instruction.Offset;
						if (!statements.ContainsKey (instruction.Offset))
							statements.Add (instruction.Offset, new Statement (instruction.Offset, "\n  unknown opcode: " + instruction.OpCode.Code + '\n'));
						else
							statements[instruction.Offset].StringValue += "\n  unknown opcode: " + instruction.OpCode.Code + '\n';
						break;
					}
				} catch (Exception e) {
					result.Append ("exception:" + e);
				}
				result.Length = 0;
				result.AppendLine ();
				int[] sortedKeys = new int [statements.Count];
				statements.Keys.CopyTo (sortedKeys, 0);
				Array.Sort (sortedKeys);
				foreach (int k in sortedKeys) {
					result.Append (String.Format("{0:X4}", k) + ":  ");
					if (statements[k].HasLabel ()) {
						result.Append (statements[k].Label + ": ");
					} else {
						result.Append ("     ");
					}
					result.Append (statements[k].StringValue);
					result.AppendLine ();
				}
			}
			result.AppendLine ();
			return result.ToString ();
		}
		
		string CallType (string call)
		{
			int end = call.IndexOf (" ");
			return call.Substring (0, end);
		}
		
		string MethodCall (Stack<Operand> stack, Instruction instruction, bool fullname)
		{
			MethodReference calledMethod = (MethodReference)instruction.Operand;
			StringBuilder result = new StringBuilder ();
			if (fullname) {
				result.Append (calledMethod.DeclaringType.FullName);
				result.Append ('.');
			}
			result.Append (calledMethod.Name);
			result.Append ('(');
			if (calledMethod.Parameters.Count > 0) {
				Operand[] op = new Operand[calledMethod.Parameters.Count];
				for (int i = calledMethod.Parameters.Count - 1; i >= 0; --i) {
					op [i] = stack.Pop ();
				}
				result.Append (op[0].StringValue);
				for (int i = 1; i < calledMethod.Parameters.Count; ++i) {
					result.Append (", ");
					result.Append (op[i].StringValue);
				}
			}
			result.Append (")");
			return result.ToString ();
		}
		
		public Decompiler ()
		{
		}
	}
}