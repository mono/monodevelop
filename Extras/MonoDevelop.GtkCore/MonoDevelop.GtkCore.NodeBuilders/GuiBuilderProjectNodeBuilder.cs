//
// GuiBuilderProjectNodeBuilder.cs
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
using System.IO;
using System.Collections;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;

namespace MonoDevelop.GtkCore.GuiBuilder
{
	public class GuiBuilderProjectNodeBuilder: TypeNodeBuilder
	{
		public override Type CommandHandlerType {
			get { return typeof(GladeFileCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/GuiBuilderProjectNodeBuilder/ContextMenu"; }
		}
			
		public override Type NodeDataType {
			get { return typeof(GuiBuilderProject); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			GuiBuilderProject bproject = (GuiBuilderProject) dataObject;
			return Path.GetFileNameWithoutExtension (bproject.File);
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			GuiBuilderProject bproject = (GuiBuilderProject) dataObject;
			label = Path.GetFileNameWithoutExtension (bproject.File);
			icon = Context.GetIcon (Stock.ResourceFileIcon);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			GuiBuilderProject bproject = (GuiBuilderProject) dataObject;
			foreach (GuiBuilderWindow fi in bproject.Windows) {
				builder.AddChild (fi);
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			GuiBuilderProject bproject = (GuiBuilderProject) dataObject;
			return bproject.Windows.Count > 0;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			GuiBuilderProject bproject = (GuiBuilderProject) dataObject;
			bproject.WindowAdded += new WindowEventHandler (OnWindowAdded);
			bproject.WindowRemoved += new WindowEventHandler (OnWindowRemoved);
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			GuiBuilderProject bproject = (GuiBuilderProject) dataObject;
			bproject.WindowAdded -= new WindowEventHandler (OnWindowAdded);
			bproject.WindowRemoved -= new WindowEventHandler (OnWindowRemoved);
		}
		
		void UpdateWidget (GuiBuilderWindow win)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (win.Project);
			if (tb != null)
				tb.UpdateAll ();
		}
		
		void OnWindowAdded (object s, WindowEventArgs a)
		{
			UpdateWidget (a.Window);
		}
		
		void OnWindowRemoved (object s, WindowEventArgs a)
		{
			UpdateWidget (a.Window);
		}
	}
	
	class GladeFileCommandHandler: NodeCommandHandler
	{
	}
}
