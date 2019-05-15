//
// NamespaceBuilder.cs
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
using System.Linq;

using MonoDevelop.Ide.Gui.Components;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using System.Threading.Tasks;

namespace MonoDevelop.AssemblyBrowser
{
	class NamespaceBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(NamespaceData); }
		}
		
		public NamespaceBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			try {
				if (thisNode == null || otherNode == null)
					return -1;
				var e1 = thisNode.DataItem as NamespaceData;
				var e2 = otherNode.DataItem as NamespaceData;
				
				if (e1 == null && e2 == null)
					return 0;
				if (e1 == null)
					return 1;
				if (e2 == null)
					return -1;
				
				return e1.Name.CompareTo (e2.Name);
			} catch (Exception e) {
				LoggingService.LogError ("Exception in assembly browser sort function.", e);
				return -1;
			}
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			NamespaceData ns = (NamespaceData)dataObject;
			return ns.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			NamespaceData ns = (NamespaceData)dataObject;
			nodeInfo.Label = GLib.Markup.EscapeText (ns.Name);
			nodeInfo.Icon = Context.GetIcon (MonoDevelop.Ide.Gui.Stock.NameSpace);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			NamespaceData ns = (NamespaceData)dataObject;
			bool publicOnly = Widget.PublicApiOnly;
			foreach (var type in ns.Types) {
				if (publicOnly && !type.isPublic)
					continue;
				ctx.AddChild (type.typeObject);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder

		public Task<List<ReferenceSegment>> DisassembleAsync (TextEditor data, ITreeNavigator navigator)
		{
		//	bool publicOnly = Widget.PublicApiOnly;
			NamespaceData ns = (NamespaceData)navigator.DataItem;
			
			data.Text = "// " + ns.Name;
			return EmptyReferenceSegmentTask;
		}
		
		public Task<List<ReferenceSegment>> DecompileAsync (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			return DisassembleAsync (data, navigator);
		}
		#endregion
	}
}
