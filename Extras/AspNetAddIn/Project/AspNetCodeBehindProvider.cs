
using System;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;

using AspNetAddIn.Parser;

namespace AspNetAddIn
{
	
	public class AspNetCodeBehindProvider: MonoDevelop.DesignerSupport.CodeBehind.ICodeBehindProvider
	{
		public IList<IClass> GetAllCodeBehindClasses (Project proj)
		{
			List<IClass> classes = new List<IClass> ();
			
			AspNetAppProject aProj = proj as AspNetAppProject;
			if (aProj == null)
				return classes;
			
			foreach (ProjectFile file in proj.ProjectFiles) {
				Document doc = aProj.GetDocument (file);
			
				if (doc == null || doc.Info.InheritedClass == null)
					continue;
			
				IParserContext ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (proj);
				IClass cls = ctx.GetClass (doc.Info.InheritedClass);
				
				if (cls != null)
					classes.Add (cls);
			}
			
			return classes;
		}
		
		public IClass GetCodeBehind (ProjectFile file)
		{
			AspNetAppProject proj = file.Project as AspNetAppProject;
			if (proj == null)
				return null;
			
			Document doc = proj.GetDocument (file);
			
			if (doc == null || doc.Info.InheritedClass == null)
				return null;
			
			IParserContext ctx = MonoDevelop.Ide.Gui.IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext (proj);
			IClass cls = ctx.GetClass (doc.Info.InheritedClass);
			
			return cls;
		}
	}
}
