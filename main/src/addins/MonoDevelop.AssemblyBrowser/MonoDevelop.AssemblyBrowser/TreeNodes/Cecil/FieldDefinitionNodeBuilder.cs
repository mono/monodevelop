//
// DomFieldNodeBuilder.cs
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
using System.Collections.Generic;
using ICSharpCode.ILSpy;
using Mono.Cecil;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.AssemblyBrowser
{
	class FieldDefinitionNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(FieldDefinition); }
		}
		
		public FieldDefinitionNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var field = (FieldDefinition)dataObject;
			return field.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var field = (FieldDefinition)dataObject;
			nodeInfo.Label = field.Name + " : " + CSharpLanguage.Instance.TypeToString (field.FieldType, false, field);

			if (!field.IsPublic)
				nodeInfo.Label = MethodDefinitionNodeBuilder.FormatPrivate (nodeInfo.Label);
			nodeInfo.Icon = Context.GetIcon (GetStockIcon(field));
		}

		public static IconId GetStockIcon (FieldDefinition field)
		{
			var isStatic = (field.Attributes & FieldAttributes.Static) != 0;
			var source = field.HasConstant ? "literal" : "field";
			var global = field.HasConstant ? "" : (isStatic ? "static-" : "");
			return "md-" + GetAccess (field.Attributes) + global + source;
		}

		static string GetAccess (FieldAttributes attributes)
		{
			switch (attributes & FieldAttributes.FieldAccessMask) {
			case FieldAttributes.Private:
				return "private-";
			case FieldAttributes.Public:
				return "";
			case FieldAttributes.Family:
				return "protected-";
			case FieldAttributes.Assembly:
				return "internal-";
			case FieldAttributes.FamORAssem:
			case FieldAttributes.FamANDAssem:
				return "ProtectedOrInternal-";
			default:
				return "";
			}
		}

		#region IAssemblyBrowserNodeBuilder

		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Disassemble (TextEditor data, ITreeNavigator navigator)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var field = (FieldDefinition)navigator.DataItem;
			if (field == null)
				return null;
			return MethodDefinitionNodeBuilder.Disassemble (data, rd => rd.DisassembleField (field));
		}
		
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Decompile (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var field = (FieldDefinition)navigator.DataItem;
			if (field == null)
				return null;
			return MethodDefinitionNodeBuilder.Decompile (data, MethodDefinitionNodeBuilder.GetAssemblyLoader (navigator), b => b.Decompile (field), flags: flags);
		}

		#endregion
	}
}
