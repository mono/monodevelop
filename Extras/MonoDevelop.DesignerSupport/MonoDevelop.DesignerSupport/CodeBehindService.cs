
using System;
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Core.AddIns;
using MonoDevelop.DesignerSupport.CodeBehind;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.DesignerSupport
{
	
	
	public class CodeBehindService
	{
		
		#region Extension loading
		
		readonly static string codeBehindProviderPath = "/MonoDevelop/DesignerSupport/CodeBehindProviders";
		List<ICodeBehindProvider> providers = new List<ICodeBehindProvider> ();
		
		internal CodeBehindService ()
		{
			Runtime.AddInService.RegisterExtensionItemListener (codeBehindProviderPath, OnProviderExtensionChanged);
		}
		
		void OnProviderExtensionChanged (ExtensionAction action, object item)
		{
			if (item == null)
				throw new Exception ("One of the CodeBehindProvider extension classes is missing");
			
			if (action == ExtensionAction.Add)
				providers.Add ((ICodeBehindProvider) item);
		}
		
		~CodeBehindService ()
		{
			Runtime.AddInService.UnregisterExtensionItemListener (codeBehindProviderPath, OnProviderExtensionChanged);
		}
		
		#endregion
		
		public IClass GetCodeBehind (ProjectFile file)
		{
			IClass match = null;
			foreach (ICodeBehindProvider provider in providers) {
				match = provider.GetCodeBehind (file);
				if (match != null)
					break;
			}
			
			return match;
		}
		
		public IList<IClass> GetAllCodeBehindClasses (Project project)
		{
			List<IClass> matches = new List<IClass> ();
			
			foreach (ICodeBehindProvider provider in providers) {
				IList<IClass> list = provider.GetAllCodeBehindClasses (project);
				if (list != null)
					foreach (IClass cls in list)
						if (!matches.Contains (cls))
							matches.Add (cls);
			}
			
			return matches;
		}
	}
}
