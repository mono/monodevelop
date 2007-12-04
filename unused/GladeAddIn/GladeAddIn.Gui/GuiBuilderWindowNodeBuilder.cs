//
// GuiBuilderWindowNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;

namespace GladeAddIn.Gui
{
	public class GuiBuilderWindowNodeBuilder: TypeNodeBuilder
	{
		public override Type CommandHandlerType {
			get { return typeof(GladeWindowCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/GladeWindowNodeBuilder/ContextMenu"; }
		}
			
		public override Type NodeDataType {
			get { return typeof(GuiBuilderWindow); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			GuiBuilderWindow win = (GuiBuilderWindow) dataObject;
			return win.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			GuiBuilderWindow win = (GuiBuilderWindow) dataObject;
			label = win.Name;
			icon = Context.GetIcon (Stock.ResourceFileIcon);
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			GuiBuilderWindow win = (GuiBuilderWindow) dataObject;
			win.NameChanged += new WindowEventHandler (OnNameChanged);
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			GuiBuilderWindow win = (GuiBuilderWindow) dataObject;
			win.NameChanged -= new WindowEventHandler (OnNameChanged);
		}
		
		void OnNameChanged (object s, WindowEventArgs a)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (a.Window);
			if (tb != null)
				tb.Update ();
		}
	}
	
	class GladeWindowCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			GuiBuilderWindow w = (GuiBuilderWindow) CurrentNode.DataItem;
			IdeApp.Workbench.OpenDocument (w.SourceCodeFile, true);
		}
	}
}
