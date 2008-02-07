//
// DomTypeNodeBuilder.cs
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

using Mono.Cecil;

using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Dom;
using MonoDevelop.Ide.Dom.Output;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.AssemblyBrowser
{
	public class DomTypeNodeBuilder : TypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IType); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			IType type = (IType)dataObject;
			return type.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IType type = (IType)dataObject;
			label = AmbienceService.Default.GetString (type, OutputFlags.ClassBrowserEntries);
			icon = Context.GetIcon (GetIcon (type));
		}
		
		static string[,] iconTable = new string[,] {
			{Stock.Error,     Stock.Error,            Stock.Error,              Stock.Error},             // unknown
			{Stock.Class,     Stock.PrivateClass,     Stock.ProtectedClass,     Stock.InternalClass},     // class
			{Stock.Enum,      Stock.PrivateEnum,      Stock.ProtectedEnum,      Stock.InternalEnum},      // enum
			{Stock.Interface, Stock.PrivateInterface, Stock.ProtectedInterface, Stock.InternalInterface}, // interface
			{Stock.Struct,    Stock.PrivateStruct,    Stock.ProtectedStruct,    Stock.InternalStruct},    // struct
			{Stock.Delegate,  Stock.PrivateDelegate,  Stock.ProtectedDelegate,  Stock.InternalDelegate}   // delegate
		};
		
		public static int GetModifierOffset (Modifiers modifier)
		{
			if ((modifier & Modifiers.Private) == Modifiers.Private)
				return 1;
			if ((modifier & Modifiers.Protected) == Modifiers.Protected)
				return 2;
			if ((modifier & Modifiers.Internal) == Modifiers.Internal)
				return 3;
			return 0;
		}
		
		public static string GetIcon (IType type)
		{
			return iconTable[(int)type.ClassType, GetModifierOffset (type.Modifiers)];
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			IType type = (IType)dataObject;
			ctx.AddChild (new BaseTypeFolder (type));
			foreach (object o in type.Members) {
				ctx.AddChild (o);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public string GetDescription (object dataObject)
		{
			IType type = (IType)dataObject;
			return AmbienceService.Default.GetString (type, OutputFlags.AssemblyBrowserDescription);
		}
		public string GetDisassembly (object dataObject)
		{
			return "TODO";
		}
		
	}
}
