
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
				
				//TODO: close file views before modifying them
				//TODO: currently case-sensitive, so some languages may not like this
				bool ignoreCase = false;
			
				if (cls != null) {
					foreach (System.CodeDom.CodeMemberField member in doc.MemberList.List.Values) {
						bool found = false;
					
						//check for identical property names
						foreach (IProperty prop in cls.Properties) {
							if (string.Compare (prop.Name, member.Name, ignoreCase) == 0) {
								monitor.ReportWarning ("Cannot add field \"" + member.Name + "\" from \"" + file.Name +
							    	                        "\" to CodeBehind file \"" + cls.BodyRegion.FileName +
							        	                    "\", because there is already a property with that name.");
								found = true;
								break;
							}
						}
					
						//check for identical method names
						foreach (IMethod meth in cls.Methods) {
							if (string.Compare (meth.Name, member.Name, ignoreCase) == 0) {
								monitor.ReportWarning ("Cannot add field \"" + member.Name + "\" from \"" + file.Name +
								                            "\" to CodeBehind file \"" + cls.BodyRegion.FileName +
							    	                        "\", because there is already a method with that name on line " + meth.BodyRegion.BeginLine + ".");
								found = true;
								break;
							}
						}
					
						//check for matching fields
						foreach (IField field in cls.Fields) {
							if (string.Compare (field.Name, member.Name, ignoreCase) == 0) {
								found = true;
							
								//check whether they're accessible
								if (!(field.IsPublic || field.IsProtected || field.IsProtectedOrInternal)) {
									monitor.ReportWarning ("Cannot add field \"" + member.Name + "\" from \"" + file.Name +
								                            "\" to CodeBehind file \"" + cls.BodyRegion.FileName +
								                            "\", because there is already a field with that name, " +
							    	                        "which cannot be accessed by the deriving page.");
									break;
								}
							
								//check they're the same type
								//TODO: check for base type compatibility
								if (string.Compare (member.Type.BaseType, field.ReturnType.FullyQualifiedName, ignoreCase) != 0) {
									monitor.ReportWarning ("Cannot add field \"" + member.Name + "\" with type \"" + member.Type.BaseType + "\" from \"" + file.Name +
								                            "\" to CodeBehind file \"" + cls.BodyRegion.FileName +
								                            "\", because there is already a field with that name, " +
								                            "which appears to have a different type, \"" + field.ReturnType.FullyQualifiedName + "\".");
									break;
								}
							}
						}
					
						if (!found)
							MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.CodeRefactorer.AddMember (cls, member);
					}
				}
			}
			
			return null;
		}
		
		public bool NeedsBuilding (Project project)
		{
			return false;
		}
	}
}
