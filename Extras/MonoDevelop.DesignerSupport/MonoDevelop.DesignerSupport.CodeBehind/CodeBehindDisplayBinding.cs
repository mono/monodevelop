//
// CodeBehindDisplayBinding.cs: Attaches secondary view of CodeBehind files.
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

using MonoDevelop.Ide;
using MonoDevelop.Ide.Codons;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Projects.Item;
using MonoDevelop.Projects.Parser;

namespace MonoDevelop.DesignerSupport.CodeBehind
{
	
	public class CodeBehindDisplayBinding : ISecondaryDisplayBinding
	{		
		public bool CanAttachTo (IViewContent content)
		{
			//FIXME: disabled, this whole thing is fundamentally broken
/*			
			IClass cls = GetCodeBehindClass (content);
			
			if (cls != null) {
				//don't attach if file is already open
				foreach (Document doc in IdeApp.Workbench.Documents)
					if (doc.FileName == cls.Region.FileName)
						return false;
				
				IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingPerFileName (cls.Region.FileName);
				if (db != null)
					return true;
			}
*/
			return false;
		}
		
		public ISecondaryViewContent CreateSecondaryViewContent (IViewContent viewContent)
		{
			IClass cls = GetCodeBehindClass (viewContent);
			
			if (cls == null)
				throw new Exception ("Cannot create CodeBehind binding for " + viewContent.ContentName + ".");
			
			IDisplayBinding db = IdeApp.Workbench.DisplayBindings.GetBindingPerFileName (cls.Region.FileName);
			IViewContent vc = db.CreateContentForFile (cls.Region.FileName);
			vc.Load (cls.Region.FileName);
			
			return new CodeBehindViewContent (vc);
		}
		
		IClass GetCodeBehindClass (IViewContent content)
		{
			if (content.Project == null)
				return null;
			
			ProjectFile file = content.Project.GetFile (content.ContentName);
			if (file == null)
				return null;
			
			return MonoDevelop.DesignerSupport.DesignerSupport.Service.CodeBehindService.GetCodeBehind (file);
		}
	}
}
