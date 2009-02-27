//
// LanguageItemNodeBuilder.cs
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2009 Levi Bard
//
// This source code is licenced under The MIT License:
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

using Mono.Addins;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;

using MonoDevelop.ValaBinding.Parser;

namespace MonoDevelop.ValaBinding.Navigation
{
	/// <summary>
	/// Class pad node builder for all Vala language items
	/// </summary>
	public class LanguageItemNodeBuilder: TypeNodeBuilder
	{
		//// <value>
		/// Container types
		/// </value>
		private static string[] containers = { "namespaces", "class", "struct", "enums" };

		//// <value>
		/// Sort order for nodes
		/// </value>
		private static string[] types = { "namespaces", "class", "struct", "property", "method", "signal", "field", "constants", "enums", "other" };
		
		public override Type NodeDataType {
			get { return typeof(CodeNode); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(LanguageItemCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((CodeNode)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder,
		                                object dataObject,
		                                ref string label,
		                                ref Gdk.Pixbuf icon,
		                                ref Gdk.Pixbuf closedIcon)
		{
			CodeNode c = (CodeNode)dataObject;
			label = c.Name;
			icon = Context.GetIcon (c.Icon);
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			ValaProject p = treeBuilder.GetParentDataItem (typeof(ValaProject), false) as ValaProject;
			if (p == null){ return; }
			
			ProjectInformation info = ProjectInformationManager.Instance.Get (p);
			
			bool publicOnly = treeBuilder.Options["PublicApiOnly"];
			CodeNode thisCodeNode = (CodeNode)dataObject;

			foreach (CodeNode child in info.GetChildren (thisCodeNode)) {
				treeBuilder.AddChild (child);
			}
		}

		/// <summary>
		/// Show all container types as having child nodes; 
		/// only check on expansion
		/// </summary>
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return (0 <= Array.IndexOf<string> (containers, ((CodeNode)dataObject).NodeType));
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (null != thisNode && null != otherNode) {
				CodeNode thisCN = thisNode.DataItem as CodeNode,
				         otherCN = otherNode.DataItem as CodeNode;
	
				if (null != thisCN && null != otherCN) {
					return Array.IndexOf<string>(types, thisCN.NodeType) - 
					       Array.IndexOf<string>(types, otherCN.NodeType);
				}
			}

			return -1;
		}
	}
}
