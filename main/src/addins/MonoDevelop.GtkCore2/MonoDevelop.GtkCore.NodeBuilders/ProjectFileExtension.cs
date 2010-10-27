using System;
using MonoDevelop.GtkCore.GuiBuilder;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.GtkCore.NodeBuilders
{
	public enum GtkComponentType 
	{
		Dialog,
		Widget,
		ActionGroup,
		IconFactory,
		None
	}
	
	public static class ProjectFileExtension
	{
		public static bool IsComponentFile (this ProjectFile pf)
		{
			return pf.GetComponentType () != GtkComponentType.None;
		}
		
		public static GtkComponentType GetComponentType (this ProjectFile pf)
		{
			GtkDesignInfo info = GtkDesignInfo.FromProject (pf.Project);
			ParsedDocument doc = ProjectDomService.GetParsedDocument (ProjectDomService.GetProjectDom (pf.Project), pf.Name);
			//ParsedDocument doc = ProjectDomService.ParseFile (ProjectDomService.GetProjectDom (pf.Project), pf.FilePath.ToString ());
			if (doc != null && doc.CompilationUnit != null) {
				foreach (IType t in doc.CompilationUnit.Types) {
					string className = t.FullName;
					if (className != null) {
						GuiBuilderWindow win = info.GuiBuilderProject.GetWindowForClass (className);
						if (win != null) 
								return win.RootWidget.IsWindow ? GtkComponentType.Dialog : GtkComponentType.Widget;
									
						Stetic.ActionGroupInfo action =	info.GuiBuilderProject.GetActionGroup (className);
						if (action != null)
							return GtkComponentType.ActionGroup;
						
					}
				}
			}
			if (pf.Name.Contains ("IconFactory.gtkx"))
				return GtkComponentType.IconFactory;
			
			return GtkComponentType.None;
		}
	}
}

