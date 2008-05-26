//
// FileTemplateReference.cs: Allows project templates to reference file 
//     templates rather than including full file templates.
//
// Author:
//     Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.Xml;

using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	
	
	public class FileTemplateReference : FileDescriptionTemplate
	{
		
		FileTemplate innerTemplate;		
		string name;
		
		public override void Load (XmlElement filenode)
		{
			name = filenode.GetAttribute ("name");
			string templateID = filenode.GetAttribute ("TemplateID");
			if (templateID == null || templateID.Length == 0)
				throw new InvalidOperationException ("TemplateID not set");
			innerTemplate = FileTemplate.GetFileTemplateByID (templateID);
			if (innerTemplate == null)
				throw new InvalidOperationException ("Could not find template with ID " + templateID);
		}

		public override string Name {
			get { return name;}
		}
		
		public override bool AddToProject (Project project, string language, string directory, string nameNotUsed)
		{
			foreach (FileDescriptionTemplate fdt in innerTemplate.Files) {
				if (!fdt.AddToProject (project, language, directory, this.name))
					return false;
			}
			return true;
		}
		
		public override void Show ()
		{
			foreach (FileDescriptionTemplate fdt in innerTemplate.Files) {
				fdt.Show ();
			}
		}
		
		public override bool IsValidName (string name, string language)
		{
			return innerTemplate.IsValidName (name, language);
		}
		
	}
}
