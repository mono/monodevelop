//
// ParentProjectFileTemplateCondition.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
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

using System;
using System.Xml;
using System.IO;

using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Templates
{
	
	public class ParentProjectFileTemplateCondition : FileTemplateCondition
	{
		bool requireExists = false;
		string relativePath = null;
		string projectType = null;
		
		public override void Load (XmlElement element)
		{
			//GetAttribute returns String.Empty even if the attr does not exist
			//but we want null (empty attribute is significant), so do an extra check
			if (element.HasAttribute ("RelativePath"))
				relativePath = element.GetAttribute("RelativePath");
			
			if (element.HasAttribute ("ProjectType"))
				projectType = element.GetAttribute("ProjectType");
			
			string requireExistsStr = element.GetAttribute ("RequireExists");
			if (!string.IsNullOrEmpty (requireExistsStr)) {
				try {
					requireExists = bool.Parse (requireExistsStr);
				} catch (FormatException) {
					throw new InvalidOperationException ("Invalid value for RequireExists in template.");
				}
			}
		}
		
		public override bool ShouldEnableFor (Project proj, string projectPath)
		{
			if (proj == null)
				return !requireExists;
			
			if (projectType != null && proj.ProjectType != projectType)
				return false;
			
			if (relativePath != null) {
				if (projectPath == null)
					return false;
				string ppath = Path.Combine (proj.BaseDirectory, relativePath);
				if (Path.GetFullPath (ppath) != Path.GetFullPath (projectPath))
					return false;
			}
			
			return true;
		}
	}
}
