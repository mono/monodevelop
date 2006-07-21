
using System;
using System.Collections.Generic;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.DesignerSupport.CodeBehind
{
	
	public interface ICodeBehindProvider
	{
		//return null if there's an error, or the project or file is unsupported
		IClass GetCodeBehind (ProjectFile file);
		IList<IClass> GetAllCodeBehindClasses (Project project);
	}
}
