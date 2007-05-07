
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
			
			List<System.CodeDom.CodeMemberField> membersToAdd = new List<System.CodeDom.CodeMemberField> ();
			List<IClass> classesForMembers = new List<IClass> ();
			
			//find the members that need to be added to CodeBehind classes
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
						MonoDevelop.Projects.Parser.IMember existingMember = BindingService.GetCompatibleMemberInClass (cls, member);
						if (existingMember == null) {
							classesForMembers.Add (cls);
							membersToAdd.Add (member);
						}
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
		/*
		class AddMembersEventArgs : System.EventArgs
		{
			public IClass Class;
			public IMember Member;
			
			AddMembersEventArgs (IClass cls, IMember member)
			{
				Class = cls;
				Member = member;
			}
		}*/
	}
}
