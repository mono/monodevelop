
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
	
	public class VerifyCodeBehindBuildStep : ProjectServiceExtension
	{
		public override ICompilerResult Build (IProgressMonitor monitor, CombineEntry project)
		{
			AspNetAppProject aspProject = project as AspNetAppProject;
			
			if (aspProject == null)
				return base.Build (monitor, project);
			
			//check the codebehind verification options
			//TODO: add options for warnings as errors, ignoring parse errors etc
			AspNetAppProjectConfiguration config = (AspNetAppProjectConfiguration) aspProject.ActiveConfiguration;
			if (!config.AutoGenerateCodeBehindMembers) {
				monitor.Log.WriteLine ("Auto-generation of CodeBehind members is disabled. Skipping CodeBehind verification.");
				return base.Build (monitor, project);
			}
			
			monitor.Log.WriteLine ("Verifying CodeBehind...");
			
			IParserContext ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (aspProject);
			ctx.UpdateDatabase ();
			
			int addedMembers = 0;
			
			foreach (ProjectFile file in aspProject.ProjectFiles) {
				WebSubtype type = AspNetAppProject.DetermineWebSubtype (Path.GetExtension (file.FilePath));
				if ((type != WebSubtype.WebForm) && (type != WebSubtype.WebControl))
						continue;
				
				Document doc = aspProject.GetDocument (file);
				if (string.IsNullOrEmpty (doc.Info.InheritedClass))
					continue;
				
				IClass cls = ctx.GetClass (doc.Info.InheritedClass);
				if (cls == null) {
					monitor.ReportWarning ("Cannot find CodeBehind class \"" + doc.Info.InheritedClass  + "\" for  file \"" + file.Name + "\".");
					continue;
				}
				
				foreach (System.CodeDom.CodeMemberField member in doc.MemberList.List.Values) {
					try {
						MonoDevelop.Projects.Parser.IMember existingMember = BindingService.GetCompatibleMemberInClass (cls, member);
						if (existingMember == null) {
							BindingService.GetCodeGenerator ().AddMember (cls, member);
							addedMembers++;
						}
					} catch (MemberExistsException m) {
						monitor.ReportWarning (m.ToString ());
					}
				}
			}
			
			if (addedMembers > 0) {
				monitor.Log.WriteLine (string.Format ("Added {0} member{1} to CodeBehind classes. Saving updated source files.", addedMembers, (addedMembers>1)?"s":""));
				
				//make sure updated files are saved before compilation
				Gtk.Application.Invoke ( delegate {
					foreach (MonoDevelop.Ide.Gui.Document guiDoc in MonoDevelop.Ide.Gui.IdeApp.Workbench.Documents)
						if (guiDoc.IsDirty)
							guiDoc.Save ();
				});
			} else {
				monitor.Log.WriteLine ("No changes made to CodeBehind classes.");
			}
			
			return base.Build (monitor, project);
		}
	}
}
