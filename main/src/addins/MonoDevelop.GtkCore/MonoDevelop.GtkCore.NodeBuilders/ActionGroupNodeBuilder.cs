//
// ActionGroupNodeBuilder.cs
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.GtkCore.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	public class ActionGroupNodeBuilder: TypeNodeBuilder
	{
		public override Type CommandHandlerType {
			get { return typeof(ActionGroupCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/GtkCore/ContextMenu/ProjectPad/ActionGroup"; }
		}
			
		public override Type NodeDataType {
			get { return typeof(Stetic.ActionGroupInfo); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			Stetic.ActionGroupInfo group = (Stetic.ActionGroupInfo) dataObject;
			return group.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Stetic.ActionGroupInfo group = (Stetic.ActionGroupInfo) dataObject;
			label = group.Name;
			icon = IdeApp.Services.Resources.GetIcon ("md-gtkcore-actiongroup", Gtk.IconSize.Menu);
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			Stetic.ActionGroupInfo group = (Stetic.ActionGroupInfo) dataObject;
			group.Changed += new EventHandler (OnChanged);
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			Stetic.ActionGroupInfo group = (Stetic.ActionGroupInfo) dataObject;
			group.Changed -= new EventHandler (OnChanged);
		}
		
		void OnChanged (object s, EventArgs a)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (s);
			if (tb != null)
				tb.Update ();
		}	
	}
	
	class ActionGroupCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			GuiBuilderWindow w = (GuiBuilderWindow) CurrentNode.GetParentDataItem (typeof(GuiBuilderWindow), false);
			if (w != null) {
				if (w.SourceCodeFile == null && !w.BindToClass ())
					return;
				
				Document doc = IdeApp.Workbench.OpenDocument (w.SourceCodeFile, true);
				if (doc != null) {
					GuiBuilderView view = doc.GetContent<GuiBuilderView> ();
					if (view != null)
						view.ShowActionDesignerView (((Stetic.ActionGroupInfo) CurrentNode.DataItem).Name);
				}
			}
			else {
				Project project = (Project) CurrentNode.GetParentDataItem (typeof(Project), false);
				Stetic.ActionGroupInfo group = (Stetic.ActionGroupInfo) CurrentNode.DataItem;
				GuiBuilderService.OpenActionGroup (project, group);
			}
		}
		
		public override bool CanDeleteItem ()
		{
			// Don't allow deleting action groups local to a window
			GuiBuilderWindow w = (GuiBuilderWindow) CurrentNode.GetParentDataItem (typeof(GuiBuilderWindow), false);
			return (w == null);
		}
		
		public override void DeleteItem ()
		{
			// Don't allow deleting action groups local to a window
			GuiBuilderWindow w = (GuiBuilderWindow) CurrentNode.GetParentDataItem (typeof(GuiBuilderWindow), false);
			if (w != null)
				return;

			Project project = (Project) CurrentNode.GetParentDataItem (typeof(Project), false);
			Stetic.ActionGroupInfo group = (Stetic.ActionGroupInfo) CurrentNode.DataItem;
			GuiBuilderProject gproject = GtkDesignInfo.FromProject (project).GuiBuilderProject;
			string sfile = gproject.GetSourceCodeFile (group);

			if (sfile != null) {
				using (ConfirmWindowDeleteDialog dialog = new ConfirmWindowDeleteDialog (group.Name, sfile, group)) {
					if (dialog.Run () == (int) Gtk.ResponseType.Yes) {
						if (dialog.DeleteFile) {
							ProjectFile file = project.GetProjectFile (sfile);
							if (file != null)
								project.Files.Remove (file);
						}
						gproject.RemoveActionGroup (group);
						gproject.Save (false);
						IdeApp.ProjectOperations.Save (project);
					}
				}
			}
		}
	}
}
