//
// DomPropertyNodeBuilder.cs
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

using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Text;
using ICSharpCode.Decompiler;
using System.Threading;
using System.Collections.Generic;
using Mono.Cecil;
using ICSharpCode.Decompiler.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using ICSharpCode.ILSpy;
using MonoDevelop.Core;

namespace MonoDevelop.AssemblyBrowser
{
	class PropertyDefinitionNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(PropertyDefinition); }
		}
		
		public PropertyDefinitionNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var property = (PropertyDefinition)dataObject;
			return property.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var property = (PropertyDefinition)dataObject;
			nodeInfo.Label = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (GetText (property, property.IsIndexer ()));

			var accessor = property.GetMethod ?? property.SetMethod;

			if (!accessor.IsPublic)
				nodeInfo.Label = MethodDefinitionNodeBuilder.FormatPrivate (nodeInfo.Label);

			nodeInfo.Icon = Context.GetIcon (GetStockIcon (property));
		}

		public static IconId GetStockIcon (PropertyDefinition property)
		{
			var accessor = property.GetMethod ?? property.SetMethod;
			return MethodDefinitionNodeBuilder.GetStockIcon (accessor);
		}

		static string GetText (PropertyDefinition property, bool? isIndexer = null)
		{
			string name = CSharpLanguage.Instance.FormatPropertyName (property, isIndexer);

			var b = new System.Text.StringBuilder ();
			if (property.HasParameters) {
				b.Append ('(');
				for (int i = 0; i < property.Parameters.Count; i++) {
					if (i > 0)
						b.Append (", ");
					b.Append (CSharpLanguage.Instance.TypeToString (property.Parameters [i].ParameterType, false, property.Parameters [i]));
				}
				var method = property.GetMethod ?? property.SetMethod;
				if (method.CallingConvention == MethodCallingConvention.VarArg) {
					if (property.HasParameters)
						b.Append (", ");
					b.Append ("...");
				}
				b.Append (") : ");
			} else {
				b.Append (" : ");
			}
			b.Append (CSharpLanguage.Instance.TypeToString (property.PropertyType, false, property));

			return name + b;
		}

		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
		
		
		#region IAssemblyBrowserNodeBuilder

		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Disassemble (TextEditor data, ITreeNavigator navigator)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var property = (PropertyDefinition)navigator.DataItem;
			return MethodDefinitionNodeBuilder.Disassemble (data, rd => rd.DisassembleProperty (property));
		}
		
		static string GetBody (string text)
		{
			int idx = text.IndexOf ('{') + 1;
			int idx2 = text.LastIndexOf ('}');
			if (idx2 - idx <= 0)
				return text;
			string result = text.Substring (idx, idx2 - idx);
			if (result.StartsWith ("\n"))
				result = result.Substring (1);
			return result;
		}

		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Decompile (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var property = navigator.DataItem as PropertyDefinition;
			if (property == null)
				return null;
			return MethodDefinitionNodeBuilder.Decompile (data, MethodDefinitionNodeBuilder.GetAssemblyLoader (navigator), b => b.Decompile (property), flags: flags);
		}
		#endregion

	}
}
