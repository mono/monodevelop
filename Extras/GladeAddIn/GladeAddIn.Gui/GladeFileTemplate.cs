
using System;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Templates;

namespace GladeAddIn.Gui
{
	public class GladeFileTemplate: FileTemplate
	{
		protected override void CreateProjectFile (Project project, string fileName, string fileContent)
		{
			GuiBuilderProject[] projects = GladeService.GetGuiBuilderProjects (project);
			GuiBuilderProject gproject = null;
			if (projects.Length == 0) {
				throw new UserException ("The project '" + project.Name + "' does not contain any glade file.");
			} else {
				gproject = projects [0];
			}
			
			fileContent = fileContent.Replace ("${GladeFile}", System.IO.Path.GetFileName (gproject.File));
		
			base.CreateProjectFile (project, fileName, fileContent);
			
			ProjectFile pf = project.ProjectFiles.GetFile (fileName);
			if (pf == null || pf.BuildAction != BuildAction.Compile)
				return;

			IParseInformation pinfo = IdeApp.ProjectOperations.ParserDatabase.UpdateFile (project, fileName, fileContent);
			if (pinfo == null) return;
			
			ICompilationUnit cunit = (ICompilationUnit)pinfo.BestCompilationUnit;
			foreach (IClass cls in cunit.Classes) {
				string id = ClassUtils.GetWindowId (cls);
				if (id != null) {
					gproject.NewDialog (id);
				}
			}
		}
	}
}
