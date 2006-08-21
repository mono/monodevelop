
using System;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport;

using AspNetAddIn.Parser;

namespace AspNetAddIn
{
	
	public class VerifyCodeBehindBuildStep : IBuildStep
	{
		
		public ICompilerResult Build (IProgressMonitor monitor, Project project)
		{
			AspNetAppProject aspProject = project as AspNetAppProject;
			
			if (aspProject == null)
				return null;
			
			IParserContext ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (project);
			ctx.UpdateDatabase ();
			
			foreach (ProjectFile file in project.ProjectFiles) {
				WebSubtype type = AspNetAppProject.DetermineWebSubtype (Path.GetExtension (file.FilePath));
				if ((type != WebSubtype.WebForm) && (type != WebSubtype.WebControl))
						continue;
				
				Document doc = aspProject.GetDocument (file);
				if (string.IsNullOrEmpty (doc.Info.InheritedClass))
					continue;
				
				IClass cls = ctx.GetClass (doc.Info.InheritedClass);
				if (cls == null) {
					monitor.ReportWarning ("Cannot find CodeBehind class \"" + doc.Info.InheritedClass  + "\" for  file \"" + file.Name + "\".");
					return null;
				}
				
				foreach (System.CodeDom.CodeMemberField member in doc.MemberList.List.Values) {
					try {
						BindingService.AddMemberToClass (cls, member, false);
					} catch (MemberExistsException m) {
						monitor.ReportWarning (m.ToString ());
					}
				}
			}
			
			//make sure changed files are saved
			Gtk.Application.Invoke ( delegate {
				foreach (MonoDevelop.Ide.Gui.Document guiDoc in MonoDevelop.Ide.Gui.IdeApp.Workbench.Documents)
					if (guiDoc.IsDirty)
						guiDoc.Save ();
			});
			
			return null;
		}
		
		public bool NeedsBuilding (Project project)
		{
			return false;
		}
	}
}
