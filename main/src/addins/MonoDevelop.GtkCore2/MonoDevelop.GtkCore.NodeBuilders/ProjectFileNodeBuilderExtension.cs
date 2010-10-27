using System;

using MonoDevelop.Components.Commands;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	public class ProjectFileBuilder : ProjectFileNodeBuilder 
	{
	
	}
	
	public class ProjectFileNodeBuilderExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof(ProjectFile).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType {
			get { return typeof (ComponentCommandHandler); }
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			if (treeNavigator.Options ["ShowAllFiles"])
				return;
			
			ProjectFile pf = (ProjectFile) dataObject;
			GtkDesignInfo info = GtkDesignInfo.FromProject (pf.Project);
			//Designer files in the designer folder like IconFactory.gtkx should be always visible
			if (info.HideGtkxFiles && 
			    pf.FilePath.Extension == ".gtkx" && 
			    !pf.FilePath.IsChildPathOf (info.SteticFolder))
				attributes |= NodeAttributes.Hidden;
		}
		
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{	
			ProjectFile pf = (ProjectFile) dataObject;
			
			//do not show custom icon for generated source files
			if (pf.DependsOn == null) {
				GtkComponentType type = pf.GetComponentType ();
				
				switch (type) {
				case GtkComponentType.Dialog : 
					icon = ImageService.GetPixbuf ("md-gtkcore-dialog", Gtk.IconSize.Menu);
					break;
				case GtkComponentType.Widget :
					icon = ImageService.GetPixbuf ("md-gtkcore-widget", Gtk.IconSize.Menu);
					break;
				case GtkComponentType.ActionGroup :
					icon = ImageService.GetPixbuf ("md-gtkcore-actiongroup", Gtk.IconSize.Menu);
					break;
				case GtkComponentType.IconFactory :
					icon = ImageService.GetPixbuf ("md-gtkcore-iconfactory", Gtk.IconSize.Menu);
					label = "Stock icons";
					break;
				}	
			}
		}
		
		//override 
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return base.HasChildNodes (builder, dataObject);
		}
	}
	
	public class ComponentCommandHandler : NodeCommandHandler
	{
		/*public override void ActivateItem ()
		{
			ProjectFile pf = (ProjectFile) CurrentNode.DataItem;
			
			if (pf.IsComponentFile ()) {
				Document doc = IdeApp.Workbench.OpenDocument (pf.FilePath, true);
				
				if (doc != null) {
					GuiBuilderView view = doc.GetContent<GuiBuilderView> ();
					if (view != null) {
						GtkComponentType type = pf.GetComponentType ();
				
						switch (type) {
						case GtkComponentType.Dialog : 
						case GtkComponentType.Widget :
							view.ShowDesignerView ();
							break;
						case GtkComponentType.ActionGroup :
							view.ShowActionDesignerView (((Stetic.ActionGroupInfo) CurrentNode.DataItem).Name);
							break;
						}
					}
				}
				return;	
			}
			base.ActivateItem ();
		}
		*/
		
		[CommandHandler (GtkCommands.GenerateCode)]
		protected void OnGenerateCode ()
		{
			ProjectFile pf = CurrentNode.DataItem as ProjectFile;
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			GuiBuilderProject gproject = info.GuiBuilderProject;
			
			gproject.GenerateCode (pf.FilePath);
		}
		
		[CommandHandler (GtkCommands.ReloadDesigner)]
		protected void OnReloadDesigner ()
		{
			ProjectFile pf = CurrentNode.DataItem as ProjectFile;
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			GtkDesignInfo info = GtkDesignInfo.FromProject (project);
			GuiBuilderProject gproject = info.GuiBuilderProject;
			
			Document doc = IdeApp.Workbench.GetDocument (pf.FilePath);
			if (doc != null) {
				gproject.ReloadFile(pf.FilePath);
				GuiBuilderView view = doc.ActiveView as GuiBuilderView;
				if (view != null) 
					view.ReloadDesigner (project);
			}
		}
		
		[CommandUpdateHandler (GtkCommands.GenerateCode)]
		[CommandUpdateHandler (GtkCommands.ReloadDesigner)]
		protected void UpdateGenerateCode (CommandInfo cinfo)
		{
			ProjectFile pf = CurrentNode.DataItem as ProjectFile;
			
			if (pf.DependsOn == null && pf.HasChildren)
				cinfo.Visible = pf.IsComponentFile ();
			else
				cinfo.Visible = false;
		}	
		
		[CommandUpdateHandler (EditCommands.Copy)]
		[CommandUpdateHandler (EditCommands.Delete)]
		[CommandUpdateHandler (EditCommands.Rename)]
		protected void UpdateDisabledCommands (CommandInfo cinfo)
		{
			//disable operations for generated files in designer folder
			cinfo.Visible = false;//(CurrentNode.GetParentDataItem (typeof (GuiProjectFolder), true) == null);
		}
		
		public override DragOperation CanDragNode ()
		{
			return DragOperation.None;
		}
		
		public override void RenameItem (string newName)
		{
			base.RenameItem (newName);
			
			Project project = CurrentNode.GetParentDataItem (typeof(Project), true) as Project;
			if (project != null) {
				ProjectFile pf = CurrentNode.DataItem as ProjectFile;
				if (pf.IsComponentFile ()) {
					GtkDesignInfo info = GtkDesignInfo.FromProject (project);
					info.RenameComponentFile (pf);
				}
			}
		}
	}
}