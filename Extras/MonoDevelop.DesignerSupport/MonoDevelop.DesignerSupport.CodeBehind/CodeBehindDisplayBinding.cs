using System;

using MonoDevelop.Ide;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.DesignerSupport.CodeBehind
{
	
	public class CodeBehindDisplayBinding : ISecondaryDisplayBinding
	{		
		public bool CanAttachTo (IViewContent content)
		{
			IClass cls = GetCodeBehindClass (content);
			
			if (cls != null) {
				IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingPerFileName (cls.Region.FileName);
				if (db != null)
					return true;
			}
			return false;
		}
		
		public ISecondaryViewContent CreateSecondaryViewContent (IViewContent viewContent)
		{
			IClass cls = GetCodeBehindClass (viewContent);
			
			if (cls == null)
				throw new Exception ("Cannot create CodeBehind binding for " + viewContent.ContentName + ".");
			
			IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingPerFileName (cls.Region.FileName);
			return new CodeBehindViewContent (db.CreateContentForFile (cls.Region.FileName));
		}
		
		IClass GetCodeBehindClass (IViewContent content)
		{
			if (content.Project == null)
				return null;
			
			ProjectFile file = content.Project.GetProjectFile (content.ContentName);
			if (file == null)
				return null;
			
			return MonoDevelop.DesignerSupport.DesignerSupport.Service.CodeBehindService.GetCodeBehind (file);
		}
	}
}
