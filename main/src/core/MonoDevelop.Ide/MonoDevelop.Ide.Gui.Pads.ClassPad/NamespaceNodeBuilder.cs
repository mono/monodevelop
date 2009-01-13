//
// NamespaceNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class NamespaceNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(NamespaceData); }
		}
		
//		ClassInformationEventHandler changeClassInformationHandler;
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Namespace"; }
		}
		
		protected override void Initialize ()
		{
/*			changeClassInformationHandler = (ClassInformationEventHandler) DispatchService.GuiDispatch (new ClassInformationEventHandler (OnClassInformationChanged));
			IdeApp.Workspace.ParserDatabase.ClassInformationChanged += changeClassInformationHandler;*/
		}
		
		public override void Dispose ()
		{
//			IdeApp.Workspace.ParserDatabase.ClassInformationChanged -= changeClassInformationHandler;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return thisNode.Options ["NestedNamespaces"] ? ((NamespaceData)dataObject).Name : ((NamespaceData)dataObject).FullName;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			NamespaceData nsData = dataObject as NamespaceData;
			label = treeBuilder.Options ["NestedNamespaces"] ? nsData.Name : nsData.FullName;
			icon = Context.GetIcon (Stock.NameSpace);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			NamespaceData nsData = dataObject as NamespaceData;

			nsData.AddProjectContent (builder);
			
			
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
	}
}
