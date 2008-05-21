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
		string projectType = null;
		
		//these specify the paths within the project in which the file may or may ne be created
		string[] permittedCreationPaths = null;
		string[] excludedCreationPaths = null;
		
		//these specify the filenames that must or must not exist within the project
		//filenames are relative to the creation directory, but rooted in the project base directory
		string[] requiredFiles = null;
		string[] excludedFiles = null;
		
		
		public override void Load (XmlElement element)
		{
			//GetAttribute returns String.Empty even if the attr does not exist
			//but we want null (empty attribute is significant), so do an extra check
			if (element.HasAttribute ("PermittedCreationPaths"))
				permittedCreationPaths = element.GetAttribute("PermittedCreationPaths").Split (':');
			
			if (element.HasAttribute ("ExcludedCreationPaths"))
				excludedCreationPaths = element.GetAttribute("ExcludedCreationPaths").Split (':');
			
			if (element.HasAttribute ("RequiredFiles"))
				requiredFiles = element.GetAttribute("RequiredFiles").Split (':');
			
			if (element.HasAttribute ("ExcludedFiles"))
				excludedFiles = element.GetAttribute("ExcludedFiles").Split (':');
			
			if (element.HasAttribute ("ProjectType"))
				projectType = element.GetAttribute("ProjectType");
			
			string requireExistsStr = element.GetAttribute ("RequireProject");
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
			
			//check for permitted creation paths
			if (permittedCreationPaths != null) {
				bool noMatches = true;
				foreach (string path in permittedCreationPaths) {
					string matchPath = Path.Combine (proj.BaseDirectory, path);
					if (Path.GetFullPath (matchPath) == Path.GetFullPath (projectPath))
						noMatches = false;
				}
				if (noMatches)
					return false;
			}
			
			//check for excluded creation paths
			if (excludedCreationPaths != null) {
				bool foundMatch = false;
				foreach (string path in excludedCreationPaths) {
					string matchPath = Path.Combine (proj.BaseDirectory, path);
					if (Path.GetFullPath (matchPath) == Path.GetFullPath (projectPath))
						foundMatch = true;
				}
				if (foundMatch)
					return false;
			}
			
			//check for required files
			if (requiredFiles != null) {
				bool requiredFileNotFound = true;
				foreach (string f in requiredFiles) {
					string requiredFile = (f[0] == '/')? 
						  Path.Combine (proj.BaseDirectory, f.Substring(1))
						: Path.Combine (projectPath, f);
					if (proj.Files.GetFile (requiredFile) != null)
						requiredFileNotFound = true;
				}
				if (requiredFileNotFound)
					return false;
			}
			
			//check for excluded files
			if (excludedFiles != null) {
				bool foundExcludedFile = false;
				foreach (string f in excludedFiles) {
					string excludedFile = (f[0] == '/')? 
						  Path.Combine (proj.BaseDirectory, f.Substring(1))
						: Path.Combine (projectPath, f);
					if (proj.Files.GetFile (excludedFile) != null)
						foundExcludedFile = true;
				}
				if (foundExcludedFile)
					return false;
			}

			
			return true;
		}
	}
}
