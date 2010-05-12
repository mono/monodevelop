// 
// SwapCodeBehindCommand.cs
//  
// Author:
//       Curtis Wensley <curtis.wensley@gmail.com>
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Curtis Wensley
// Copyright (c) 2010 Novell, Inc.
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
using MonoDevelop.Components.Commands;
using System.IO;
using System.Linq;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using MonoDevelop.Ide;

namespace MonoDevelop.DesignerSupport
{
	public class SwitchBetweenRelatedFilesCommand : CommandHandler
	{
		IEnumerable<ProjectFile> GetFileGroup (ProjectFile currentFile)
		{
			var parent = currentFile.DependsOnFile ?? currentFile;
			var children = parent.DependentChildren;
			if (children != null) {
				yield return parent;
				foreach (var c in children)
					if (!CodeBehind.IsDesignerFile (c.FilePath))
						yield return c;
			}
		}

		protected override void Run ()
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			var projectFile = doc.Project.GetProjectFile (doc.FileName);
			var files = GetFileGroup (projectFile).ToList ();
			for (int i = 0; i < files.Count; i++) {
				if (projectFile.Equals (files[i]))
					IdeApp.Workbench.OpenDocument (files[(i+1)%(files.Count)].FilePath, true);
			}
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = info.Visible = false;
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.Project == null)
				return;
			
			var projectFile = doc.Project.GetProjectFile (doc.FileName);
			info.Enabled = info.Visible = projectFile != null && GetFileGroup (projectFile).Count () > 1;
		}
	}
}
