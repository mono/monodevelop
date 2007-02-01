
using System;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;

using AspNetAddIn.Parser;

namespace AspNetAddIn
{
	
	public class AspNetCodeBehindProvider: MonoDevelop.DesignerSupport.CodeBehind.ICodeBehindProvider
	{
		
		public string GetCodeBehindClassName (ProjectFile file)
		{
			AspNetAppProject proj = file.Project as AspNetAppProject;
			if (proj == null)
				return null;
			
			Document doc = proj.GetDocument (file);
			
			if (doc != null)
				return doc.Info.InheritedClass;
			
			return null;
		}
	}
}
