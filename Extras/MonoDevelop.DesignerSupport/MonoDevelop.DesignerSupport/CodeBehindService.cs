//
// CodeBehindService.cs: Links codebehind classes to their parent files.
//
// Authors:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (C) 2006 Michael Hutchinson
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

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
