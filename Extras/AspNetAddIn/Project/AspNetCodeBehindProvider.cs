
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
			return AspNetAddIn.CodeBehind.GetCodeBehindClassName (file);
		}
	}
}
