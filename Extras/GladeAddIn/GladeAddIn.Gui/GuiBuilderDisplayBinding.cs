
using System;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;

namespace GladeAddIn.Gui
{
	public class GuiBuilderDisplayBinding: IDisplayBinding
	{
		bool excludeThis = false;
		
		public virtual bool CanCreateContentForFile (string fileName)
		{
			if (excludeThis) return false;
			
			if (GetWindow (fileName) == null)
				return false;
			
			excludeThis = true;
			IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingPerFileName (fileName);
			excludeThis = false;
			return db != null;
		}

		public virtual bool CanCreateContentForMimeType (string mimetype)
		{
			return false;
		}
		
		public virtual IViewContent CreateContentForFile (string fileName)
		{
			excludeThis = true;
			IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingPerFileName (fileName);
			GuiBuilderView view = new GuiBuilderView (db.CreateContentForFile (fileName), GetWindow (fileName));
			excludeThis = false;
			view.Load (fileName);
			return view;
		}
		
		public virtual IViewContent CreateContentForMimeType (string mimeType, string content)
		{
			return null;
		}
		
		GuiBuilderWindow GetWindow (string file)
		{
			if (IdeApp.ProjectOperations.CurrentOpenCombine == null)
				return null;

			Project project = null;
			foreach (Project p in IdeApp.ProjectOperations.CurrentOpenCombine.GetAllProjects ()) {
				if (p.IsFileInProject (file)) {
					project = p;
					break;
				}
			}
			
			if (project == null)
				return null;

			GuiBuilderProject[] gprojects = GladeService.GetGuiBuilderProjects (project);
			foreach (GuiBuilderProject gproject in gprojects) {
				foreach (GuiBuilderWindow win in gproject.Windows) {
					if (win.SourceCodeFile == file)
						return win;
				}
			}

			return null;
		}
	}
}
