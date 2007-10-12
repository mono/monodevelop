
using System;
using System.IO;
using System.Collections.Generic;

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
			
			//lists of members to be added 
			List<System.CodeDom.CodeMemberField> membersToAdd = new List<System.CodeDom.CodeMemberField> ();
			List<IClass> classesForMembers = new List<IClass> ();
			
			AspNetAppProjectConfiguration config = (AspNetAppProjectConfiguration) aspProject.ActiveConfiguration;
			
			//get an updated parser database
			IParserContext ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (aspProject);
			ctx.UpdateDatabase ();
			
			monitor.Log.WriteLine ("Generating CodeBehind members...");
			if (!config.GenerateNonPartialCodeBehindMembers)
				monitor.Log.WriteLine ("Auto-generation of CodeBehind members is disabled for non-partial classes.");
			
			//find the members that need to be added to CodeBehind classes
			foreach (ProjectFile file in aspProject.ProjectFiles) {
				
				WebSubtype type = AspNetAppProject.DetermineWebSubtype (Path.GetExtension (file.FilePath));
				if ((type != WebSubtype.WebForm) && (type != WebSubtype.WebControl) && (type != WebSubtype.MasterPage))
						continue;
				
				string className = CodeBehind.GetCodeBehindClassName (file);
				if (className == null)
					continue;
				
				IClass cls = ctx.GetClass (className);
				if (cls == null) {
					monitor.ReportWarning ("Cannot find CodeBehind class \"" + className  + "\" for  file \"" + file.Name + "\".");
					continue;
				}
				
				//handle partial designer classes; skip if non-partial and this is disabled
				IClass partToAddTo = CodeBehind.GetDesignerClass (cls);
				if (partToAddTo == null) {
					if (!config.GenerateNonPartialCodeBehindMembers)
						continue;
					else
						partToAddTo = cls;
				}
				
				Document doc = null; 
				try {
					doc = aspProject.GetDocument (file);
				} catch (Exception e) {
					monitor.ReportWarning (string.Format ("Parser failed on {0} with error {1}. CodeBehind members for this file will not be added.", file, e.ToString ()));
				}
				
				try {
					if (doc != null) {
						foreach (System.CodeDom.CodeMemberField member in doc.MemberList.List.Values) {
							MonoDevelop.Projects.Parser.IMember existingMember = BindingService.GetCompatibleMemberInClass (cls, member);
							if (existingMember == null) {
								classesForMembers.Add (partToAddTo);
								membersToAdd.Add (member);
							}
						}
					}
				} catch (Exception e) {
					monitor.ReportWarning (string.Format ("CodeBehind member generation failed on {0} with error {1}. Further CodeBehind members for this file will not be added.", file, e.ToString ()));
				}
			}
			
			//add all the members
			//documents may be open, so needs to run in GUI thread
			Gtk.Application.Invoke ( delegate {
				for (int i = 0; i < membersToAdd.Count; i++)
					try {
						BindingService.GetCodeGenerator ().AddMember (classesForMembers[i], membersToAdd[i]);
					} catch (MemberExistsException m) {
						monitor.ReportWarning (m.ToString ());
					}			
			});

			if (membersToAdd.Count > 0) {
				monitor.Log.WriteLine (string.Format ("Added {0} member{1} to CodeBehind classes. Saving updated source files.", membersToAdd.Count, (membersToAdd.Count>1)?"s":""));
				
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
