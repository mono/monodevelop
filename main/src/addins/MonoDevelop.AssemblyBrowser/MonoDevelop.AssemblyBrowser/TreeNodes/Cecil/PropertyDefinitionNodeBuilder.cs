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
using ICSharpCode.Decompiler.TypeSystem;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.AssemblyBrowser
{
	class PropertyDefinitionNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IProperty); }
		}
		
		public PropertyDefinitionNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var property = (IProperty)dataObject;
			return property.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var property = (IProperty)dataObject;
			nodeInfo.Label = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (property.GetDisplayString ());

			var accessor = property.Getter ?? property.Setter;

			if (!accessor.IsPublic ())
				nodeInfo.Label = MethodDefinitionNodeBuilder.FormatPrivate (nodeInfo.Label);

			nodeInfo.Icon = Context.GetIcon (GetStockIcon (property));
		}

		public static IconId GetStockIcon (IProperty property)
		{
			var accessor = property.Getter ?? property.Setter;
			var global = accessor.IsStatic ? "static-" : "";
			return "md-" + accessor.Accessibility.GetStockIcon () + global + "property";
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}


		#region IAssemblyBrowserNodeBuilder

		Task<List<ReferenceSegment>> IAssemblyBrowserNodeBuilder.DisassembleAsync (TextEditor data, ITreeNavigator navigator)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return EmptyReferenceSegmentTask;
			var property = (IProperty)navigator.DataItem;
			return MethodDefinitionNodeBuilder.DisassembleAsync (data, rd => rd.DisassembleProperty (property.ParentModule.PEFile, (System.Reflection.Metadata.PropertyDefinitionHandle)property.MetadataToken));
		}

		Task<List<ReferenceSegment>> IAssemblyBrowserNodeBuilder.DecompileAsync (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return EmptyReferenceSegmentTask;
			if (!(navigator.DataItem is IProperty property))
				return EmptyReferenceSegmentTask;
			return MethodDefinitionNodeBuilder.DecompileAsync (data, MethodDefinitionNodeBuilder.GetAssemblyLoader (navigator), b => b.Decompile (property.MetadataToken), flags: flags);
		}
		#endregion

	}
}
