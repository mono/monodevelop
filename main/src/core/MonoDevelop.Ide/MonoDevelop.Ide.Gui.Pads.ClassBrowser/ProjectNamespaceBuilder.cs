//
// ProjectNamespaceBuilder.cs
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Components;


namespace MonoDevelop.Ide.Gui.Pads.ClassBrowser
{
	public class ProjectNamespaceNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(MonoDevelop.Projects.Dom.Namespace); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Namespace"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			MonoDevelop.Projects.Dom.Namespace ns = (MonoDevelop.Projects.Dom.Namespace)dataObject;
			return ns.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			MonoDevelop.Projects.Dom.Namespace ns = (MonoDevelop.Projects.Dom.Namespace)dataObject;
			label = ns.Name;
			icon  = Context.GetIcon (ns.StockIcon);
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			MonoDevelop.Projects.Dom.Namespace ns = (MonoDevelop.Projects.Dom.Namespace)dataObject;
			Project project =  (Project)builder.GetParentDataItem (typeof(Project), false);
			ProjectDom dom = ProjectDomService.GetDatabaseProjectDom (project);
			
			foreach (MonoDevelop.Projects.Dom.IMember member in dom.GetNamespaceContents (ns.Name, false, true)) {
				builder.AddChild (member);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}		
	}
}
