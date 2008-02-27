
using System;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;

using MonoDevelop.AspNet.Parser;

namespace MonoDevelop.AspNet
{
	
	public class AspNetCodeBehindProvider: MonoDevelop.DesignerSupport.CodeBehind.ICodeBehindProvider
	{
		
		public string GetCodeBehindClassName (ProjectFile file)
		{
			return CodeBehind.GetCodeBehindClassName (file);
		}
	}
}
