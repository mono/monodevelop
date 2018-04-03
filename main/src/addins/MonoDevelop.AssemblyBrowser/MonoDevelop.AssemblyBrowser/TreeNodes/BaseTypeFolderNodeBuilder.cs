//
// BaseTypeFolderNodeBuilder.cs
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

using Mono.Cecil;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;
using System.Linq;
using ICSharpCode.Decompiler.TypeSystem;

namespace MonoDevelop.AssemblyBrowser
{
	class BaseTypeNodeBuilder : AssemblyBrowserTypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ITypeReference); }
		}
		
		public BaseTypeNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var reference = dataObject as ITypeReference;
			return reference.ToString ();
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var reference = dataObject as ITypeReference;
			nodeInfo.Label = reference.ToString ();
			nodeInfo.Icon = Context.GetIcon (Stock.Class);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			var r1 = thisNode.DataItem as ITypeReference;
			var r2 = thisNode.DataItem as ITypeReference;
			return r1.ToString ().CompareTo (r2.ToString ());
		}
	}
	
	class BaseTypeFolderNodeBuilder : AssemblyBrowserTypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(BaseTypeFolder); }
		}
		
		public BaseTypeFolderNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "Base Types";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			nodeInfo.Label      = MonoDevelop.Core.GettextCatalog.GetString ("Base Types");
			nodeInfo.Icon       = Context.GetIcon (Stock.OpenFolder);
			nodeInfo.ClosedIcon = Context.GetIcon (Stock.ClosedFolder);
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			var baseTypeFolder = (BaseTypeFolder)dataObject;
			builder.AddChildren (baseTypeFolder.Type.BaseTypes);
		}
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override int GetSortIndex (ITreeNavigator node)
		{
			return -100;
		}
	}
}
