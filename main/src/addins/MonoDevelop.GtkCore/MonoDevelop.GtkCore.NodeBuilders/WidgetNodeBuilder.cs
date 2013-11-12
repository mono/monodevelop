//
// WidgetNodeBuilder.cs
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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.GtkCore.Dialogs;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	public class WidgetNodeBuilder: TypeNodeBuilder
	{
		readonly WindowEventHandler onChanged;
		
		public WidgetNodeBuilder ()
		{
			onChanged = new WindowEventHandler (OnChanged);
		}
		
		public override Type CommandHandlerType {
			get { return typeof(GladeWindowCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/GtkCore/ContextMenu/ProjectPad.Component"; }
		}
			
		public override Type NodeDataType {
			get { return typeof(GuiBuilderWindow); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var win = (GuiBuilderWindow) dataObject;
			return win.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			var win = (GuiBuilderWindow) dataObject;
			label = win.Name;
			
//			if (win.RootWidget.IsWindow)
//				icon = ImageService.GetPixbuf ("md-gtkcore-window");
			icon = ImageService.GetPixbuf (win.RootWidget.IsWindow ? "md-gtkcore-dialog" : "md-gtkcore-widget", Gtk.IconSize.Menu);
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var win = (GuiBuilderWindow) dataObject;
			foreach (Stetic.ActionGroupInfo agroup in win.RootWidget.ActionGroups) {
				treeBuilder.AddChild (agroup);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var win = (GuiBuilderWindow) dataObject;
			return win.RootWidget.ActionGroups.GetEnumerator().MoveNext ();
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			var win = (GuiBuilderWindow) dataObject;
			win.Changed += onChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			var win = (GuiBuilderWindow) dataObject;
			win.Changed -= onChanged;
		}
		
		void OnChanged (object s, WindowEventArgs a)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (a.Window);
			if (tb != null)
				tb.UpdateAll ();
		}
	}
	
	class GladeWindowCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			var w = (GuiBuilderWindow) CurrentNode.DataItem;
			if (w.SourceCodeFile == FilePath.Null && !w.BindToClass ())
				return;
			
			Document doc = IdeApp.Workbench.OpenDocument (w.SourceCodeFile, true);
			if (doc != null) {
				GuiBuilderView view = doc.GetContent<GuiBuilderView> ();
				if (view != null)
					view.ShowDesignerView ();
			}
		}
		
		public override void DeleteItem ()
		{
			var w = (GuiBuilderWindow) CurrentNode.DataItem;
			string fn = FileService.AbsoluteToRelativePath (w.Project.Project.BaseDirectory, w.SourceCodeFile);
			using (var dialog = new ConfirmWindowDeleteDialog (w.Name, fn, w.RootWidget)) {
				if (dialog.Run () == (int) Gtk.ResponseType.Yes) {
					if (dialog.DeleteFile) {
						ProjectFile file = w.Project.Project.GetProjectFile (w.SourceCodeFile);
						if (file != null)
							w.Project.Project.Files.Remove (file);
					}
					w.Project.Remove (w);
					w.Project.SaveProject (false);
					IdeApp.ProjectOperations.Save (w.Project.Project);
				}
			}
		}
	}
}
