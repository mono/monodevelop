// 
// DirectoryTemplate.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Templates
{
	public class DirectoryTemplate : FileDescriptionTemplate
	{
		readonly List<FileDescriptionTemplate> templates = new List<FileDescriptionTemplate> ();
		string dirName;
		
		public override string Name {
			get { return dirName; } 
		}
		
		public override void Load (XmlElement filenode, FilePath baseDirectory)
		{
			dirName = filenode.GetAttribute ("name");
			if (string.IsNullOrEmpty (dirName) || !FileService.IsValidFileName (dirName))
				throw new InvalidOperationException ("Invalid name in directory template");
			
			foreach (XmlNode node in filenode) {
				if (!(node is XmlElement))
					continue;
				
				FileDescriptionTemplate t = FileDescriptionTemplate.CreateTemplate ((XmlElement)node, baseDirectory);
				if (t == null)
					throw new InvalidOperationException ("Invalid file template in directory template");
				
				templates.Add (t);
			}
		}
		
		public override bool SupportsProject (Project project, string projectPath)
		{
			if (project == null)
				return false;
			
			foreach (FileDescriptionTemplate t in templates)
				if (!t.SupportsProject (project, projectPath))
					return false;
			return true;
		}
		
		public override bool IsValidName (string name, string language)
		{
			foreach (FileDescriptionTemplate t in templates)
				if (!t.IsValidName (name, language))
					return false;
			return true;
		}
		
		public override void Show ()
		{
			foreach (FileDescriptionTemplate t in templates)
				t.Show ();
		}
			
		public override async Task<bool> AddToProjectAsync (SolutionFolderItem policyParent, Project project,
		                                   string language, string directory, string name)
		{
			bool addedSomething = false;
			directory = Path.Combine (directory, dirName);
			if (templates.Count == 0) {
				string relPath = FileService.AbsoluteToRelativePath (project.BaseDirectory, directory);
				if (project.AddDirectory (relPath) != null)
					addedSomething = true;
			}
			
			foreach (FileDescriptionTemplate t in templates) {
				if (t.EvaluateCreateCondition ()) {
					addedSomething |= await t.AddToProjectAsync (policyParent, project, language, directory, name);
				}
			}
			return addedSomething;
		}

		internal override void SetProjectTagModel (MonoDevelop.Core.StringParsing.IStringTagModel tagModel)
		{
			base.SetProjectTagModel (tagModel);
			foreach (FileDescriptionTemplate t in templates) {
				t.SetProjectTagModel (tagModel);
			}
		}
	}
}
