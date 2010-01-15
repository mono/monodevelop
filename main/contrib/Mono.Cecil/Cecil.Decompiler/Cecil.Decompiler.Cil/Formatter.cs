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
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Cecil.Decompiler.Cil {

	public static class Formatter {

		public static string FormatInstruction (Instruction instruction)
		{
			StringWriter writer = new StringWriter ();
			WriteInstruction (writer, instruction);
			return writer.ToString ();
		}

		public static string FormatMethodBody (MethodDefinition method)
		{
			StringWriter writer = new StringWriter ();
			WriteMethodBody (writer, method);
			return writer.ToString ();
		}

		public static void WriteMethodBody (TextWriter writer, MethodDefinition method)
		{
			writer.WriteLine (method);
			foreach (Instruction instruction in method.Body.Instructions) {
				writer.Write ('\t');
				WriteInstruction (writer, instruction);
				writer.WriteLine ();
			}
		}

		public static void WriteInstruction (TextWriter writer, Instruction instruction)
		{
			writer.Write (FormatLabel (instruction.Offset));
			writer.Write (": ");
			writer.Write (instruction.OpCode.Name);
			if (null != instruction.Operand) {
				writer.Write (' ');
				WriteOperand (writer, instruction.Operand);
			}
		}

		private static string FormatLabel (int offset)
		{
			string label = "000" + offset.ToString ("x");
			return "IL_" + label.Substring (label.Length - 4);
		}

		private static void WriteOperand (TextWriter writer, object operand)
		{
			if (null == operand) throw new ArgumentNullException ("operand");

			Instruction targetInstruction = operand as Instruction;
			if (null != targetInstruction) {
				writer.Write (FormatLabel (targetInstruction.Offset));
				return;
			}

			Instruction [] targetInstructions = operand as Instruction [];
			if (null != targetInstructions) {
				WriteLabelList (writer, targetInstructions);
				return;
			}

			VariableReference variableRef = operand as VariableReference;
			if (null != variableRef) {
				writer.Write (variableRef.Index.ToString ());
				return;
			}

			MethodReference methodRef = operand as MethodReference;
			if (null != methodRef) {
				WriteMethodReference (writer, methodRef);
				return;
			}

			string s = operand as string;
			if (null != s) {
				writer.Write ("\"" + s + "\"");
				return;
			}

			s = ToInvariantCultureString (operand);
			writer.Write (s);
		}

		static void WriteLabelList (TextWriter writer, Instruction [] instructions)
		{
			writer.Write ("(");

			for (int i = 0; i < instructions.Length; i++) {
				if (i != 0) writer.Write (", ");
				writer.Write (FormatLabel (instructions [i].Offset));
			}

			writer.Write (")");
		}

		public static string ToInvariantCultureString (object value)
		{
			IConvertible convertible = value as IConvertible;
			return (null != convertible)
				? convertible.ToString (System.Globalization.CultureInfo.InvariantCulture)
				: value.ToString ();
		}

		private static void WriteMethodReference (TextWriter writer, MethodReference method)
		{
			writer.Write (FormatTypeReference (method.ReturnType.ReturnType));
			writer.Write (' ');
			writer.Write (FormatTypeReference (method.DeclaringType));
			writer.Write ("::");
			writer.Write (method.Name);
			writer.Write ("(");
			ParameterDefinitionCollection parameters = method.Parameters;
			for (int i=0; i < parameters.Count; ++i) {
				if (i > 0) writer.Write (", ");
				writer.Write (FormatTypeReference (parameters [i].ParameterType));
			}
			writer.Write (")");
		}

		public static string FormatTypeReference (TypeReference type)
		{
			string typeName = type.FullName;
			switch (typeName) {
			case "System.Void": return "void";
			case "System.String": return "string";
			case "System.Int32": return "int32";
			case "System.Long": return "int64";
			case "System.Boolean": return "bool";
			case "System.Single": return "float32";
			case "System.Double": return "float64";
			}
			return typeName;
		}
	}
}
