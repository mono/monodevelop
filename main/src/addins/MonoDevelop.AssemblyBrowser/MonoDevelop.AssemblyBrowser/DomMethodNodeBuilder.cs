//
// DomMethodNodeBuilder.cs
//
// Author:
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

using Mono.Cecil;
using Mono.Cecil.Cil;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Dom;
using MonoDevelop.Ide.Dom.Output;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.AssemblyBrowser.Dom;

namespace MonoDevelop.AssemblyBrowser
{
	public class DomMethodNodeBuilder : TypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		static readonly string[] iconTable = {Stock.Method, Stock.PrivateMethod, Stock.ProtectedMethod, Stock.InternalMethod};
		
		public override Type NodeDataType {
			get { return typeof(IMethod); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			IMethod method = (IMethod)dataObject;
			return method.FullName;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IMethod method = (IMethod)dataObject;
			label = AmbienceService.Default.GetString (method, OutputFlags.ClassBrowserEntries);
			icon = Context.GetIcon (iconTable[DomTypeNodeBuilder.GetModifierOffset (method.Modifiers)]);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is BaseTypeFolder)
				return 1;
			if (otherNode.DataItem is IMethod)
				return ((IMethod)thisNode.DataItem).Name.CompareTo (((IMethod)otherNode.DataItem).Name);
			
			return -1;
		}
		
		#region IAssemblyBrowserNodeBuilder
		internal static void PrintDeclaringType (StringBuilder result, ITreeNavigator navigator)
		{
			IType type = (IType)navigator.GetParentDataItem (typeof (IType), false);
			if (type == null)
				return;
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Declaring Type:</b>\t{0}"), type.FullName));
			result.AppendLine ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			IMethod method = (IMethod)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append ("<span font_family=\"monospace\">");
			result.Append (AmbienceService.Default.GetString (method, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</span>");
			result.AppendLine ();
			PrintDeclaringType (result, navigator);
			DomTypeNodeBuilder.PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		static string GetInstructionOffset (Instruction instruction)
		{
			return String.Format ("IL_{0:X4}", instruction.Offset);
		}
		
		string IAssemblyBrowserNodeBuilder.GetDisassembly (ITreeNavigator navigator)
		{
			DomCecilMethod method = navigator.DataItem as DomCecilMethod;
			if (method == null)
				return "";
			if (method.MethodDefinition.IsPInvokeImpl)
				return GettextCatalog.GetString ("Method is P/Invoke");
			if (method.MethodDefinition.Body == null) {
				IType type = (IType)navigator.GetParentDataItem (typeof (IType), false);
				return type == null || type.ClassType == ClassType.Interface ? GettextCatalog.GetString ("Interface method") : GettextCatalog.GetString ("Abstract method");
			}
			
			StringBuilder result = new StringBuilder ();
			foreach (Instruction instruction in method.MethodDefinition.Body.Instructions ) {
				result.Append (GetInstructionOffset (instruction));
				result.Append (' ');
				result.Append (instruction.OpCode);
				if (instruction.Operand != null) {
					result.Append (' ');
					if (instruction.Operand is string) {
						result.Append ('"');
						result.Append (instruction.Operand);
						result.Append ('"');
					} else if (instruction.Operand is Mono.Cecil.Cil.Instruction) {
						result.Append (GetInstructionOffset ((Mono.Cecil.Cil.Instruction)instruction.Operand));
					} else {
						result.Append (instruction.Operand);
					}
				}
				result.AppendLine ();
			}
					
			return result.ToString ();
		}
		#endregion

	}
}
