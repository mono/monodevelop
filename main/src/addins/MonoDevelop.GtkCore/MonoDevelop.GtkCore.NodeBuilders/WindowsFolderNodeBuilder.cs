//
// WindowsFolderNodeBuilder.cs
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
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	public class WindowsFolderNodeBuilder: TypeNodeBuilder
	{
		EventHandler updateDelegate;
		
		public WindowsFolderNodeBuilder ()
		{
			updateDelegate = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnUpdateFiles));
		}
		
		public override Type NodeDataType {
			get { return typeof(WindowsFolder); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(UserInterfaceCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/GtkCore/ContextMenu/ProjectPad.UserInterfaceFolder"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "UserInterface";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Project p = ((WindowsFolder)dataObject).Project;
			GtkDesignInfo info = GtkDesignInfo.FromProject (p);
			if (info.GuiBuilderProject.HasError) {
				label = GettextCatalog.GetString ("User Interface (GUI project load failed)");
			} else {
				label = GettextCatalog.GetString ("User Interface");
			}
			icon = Context.GetIcon (Stock.OpenResourceFolder);
			closedIcon = Context.GetIcon (Stock.ClosedResourceFolder);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project p = ((WindowsFolder)dataObject).Project;
			GtkDesignInfo info = GtkDesignInfo.FromProject (p);
			if (!info.GuiBuilderProject.HasError) {
				builder.AddChild (new StockIconsNode (p));
				foreach (GuiBuilderWindow fi in info.GuiBuilderProject.Windows)
					builder.AddChild (fi);
				foreach (Stetic.ActionGroupInfo group in info.GuiBuilderProject.SteticProject.ActionGroups)
					builder.AddChild (group);
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is ProjectReferenceCollection)
				return 1;
			else
				return -1;
		}

		public override void OnNodeAdded (object dataObject)
		{
			WindowsFolder w = (WindowsFolder) dataObject;
			w.Changed += updateDelegate;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			WindowsFolder w = (WindowsFolder)dataObject;
			w.Changed -= updateDelegate;
			w.Dispose ();
		}
		
		void OnUpdateFiles (object s, EventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (s);
			if (tb != null) {
				tb.UpdateAll ();
			}
		}
	}
}
